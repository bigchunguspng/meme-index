using ColorHelper;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Directory = System.IO.Directory;

namespace MemeIndex_Core.Services.ImageToText.ColorTag;

public class ColorTagService(ColorSearchProfile colorSearchProfile) : IImageToTextService
{
#if DEBUG
    private readonly JpegEncoder _defaultJpegEncoder = new() { Quality = 80 };
#endif

    public event Action<Dictionary<string, List<RankedWord>>>? ImageProcessed;

    public async Task ProcessFiles(IEnumerable<string> paths)
    {
        var tasks = paths.Select(async path =>
        {
            var rankedWords = await GetTextRepresentation(path);
            if (rankedWords is null) return;

            Logger.Log(ConsoleColor.Blue, $"COLOR-TAG: words: {rankedWords.Count,4}");

            var result = new Dictionary<string, List<RankedWord>> { { path, rankedWords } };
            ImageProcessed?.Invoke(result);
        });

        await Task.WhenAll(tasks);
    }

    // todo:
    // - replace dot grid with line grid algorithm  (still relevant???xd)

    public async Task<List<RankedWord>?> GetTextRepresentation(string path)
    {
        try
        {
            // CHECK FILE
            var file = new FileInfo(path);
            if (file.Exists == false)
            {
                return null;
            }

            var sw = Helpers.GetStartedStopwatch();

            ImageScanResult scanResult;

            using (var image = await Image.LoadAsync<Rgba32>(path))
            {
                sw.Log("IMAGE LOADED");

                scanResult = ScanImage(image);

                sw.Log($"IMAGE SCANNED ({scanResult.TotalSamples} samples collected)");

#if DEBUG
                var ticks = DateTime.UtcNow.Ticks;
                var name = $"baka-{path.FancyHashCode()}-{ticks >> 32}";
                Directory.CreateDirectory("img");
                await image.SaveAsJpegAsync(Path.Combine("img", $"{name}-dots.jpg"), _defaultJpegEncoder);

                sw.Log("DEBUG-DOTS EXPORTED");
#endif
            }

            var rankedWords = AnalyzeImageSamples(scanResult);

            sw.Log("ANALYSIS DONE");

            return rankedWords.OrderByDescending(x => x.Rank).ToList();

        }
        catch (Exception e)
        {
            Logger.LogError(nameof(ColorTagService), e);
            return null;
        }
    }

    private ImageScanResult ScanImage(Image<Rgba32> image)
    {
        var step = CalculateStep(image.Size);

        var samplesGrayscale = new ColorFrequency();
        var samplesFunny = colorSearchProfile.Hues.Select(_ => new ColorFrequency()).ToArray();

        var totalOpacity = 0;

        var samplesCollected = 0;
        for (var x = 0; x < image.Width;  x += step)
        for (var y = 0; y < image.Height; y += step)
        {
            samplesCollected++;

            var sample = image[x, y];

            totalOpacity += sample.A;

            if (sample.A < 8) continue; // discard almost invisible pixels

            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var s = hsl.S;
            var l = hsl.L;

            var hue = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues

#if DEBUG
                image[x, y] = Color.Red;
#endif

            var point = new BytePoint(s, l);

            var isGrayscale = s < 10 || l is < 4 or > 96;
            var sampleTable = isGrayscale ? samplesGrayscale : samplesFunny[hue];

            sampleTable.AddOrIncrement(point);
        }

        var transparency = samplesCollected * byte.MaxValue - totalOpacity;

        return new ImageScanResult(samplesCollected, transparency, samplesGrayscale, samplesFunny);
    }

    private static int CalculateStep(Size size)
    {
        // 720 x 720 => 16
        // 180 x 180 =>  4

        var area = size.Width * size.Height;
        var step = (int)Math.Clamp(Math.Sqrt(area / 2025D), 2, 32);

        Logger.Log($"[step = {step}]");

        return step;
    }

    /*
        0   5       40             100
        +---+--------+---------------+  0
        |              WHITE / BLACK |
        +---+--------+---------------+  4 / 96
        | G |                        |
        | R +--------+  DARK / LIGHT | 12 / 88
        | A |        |               |
        | Y |        +---------------+ 20 / 80
        |   |  PALE  |               |
        |   |        |    VIBRANT    |
        |   |        |               |
        +---+--------+---------------+ 50
        saturation -->
     */

    private const int X_GRAY = 5, X_PALE = 40;
    private const int Y_BLACK = 04, Y_WHITE = 96;
    private const int Y_DIM_D = 12, Y_DIM_L = 88;
    private const int Y_VIB_D = 20, Y_VIB_L = 80;

    private List<RankedWord> AnalyzeImageSamples(ImageScanResult data)
    {
        var samplesTotal = (double)data.TotalSamples;
        var threshold = (int)Math.Round(Math.Log2(samplesTotal / 1000));
        var result = new List<RankedWord>();

        Logger.Log($"[threshold = {threshold}]");

        // GRAYSCALE

        var white = data.Grayscale.Where(x => x.Key.Y > Y_WHITE);
        var black = data.Grayscale.Where(x => x.Key.Y < Y_BLACK);

        var gray = data.Grayscale.Where(x => x.Key is { X: < X_GRAY, Y: >= Y_BLACK and <= Y_WHITE }).ToList();

        var y4 = gray.Where(x => x.Key is { Y:           < 27 });
        var y3 = gray.Where(x => x.Key is { Y: >= 27 and < 50 });
        var y2 = gray.Where(x => x.Key is { Y: >= 50 and < 73 });
        var y1 = gray.Where(x => x.Key is { Y: >= 73 });

        var codes = colorSearchProfile.ColorsGrayscale.Keys.ToArray();
        var g = 0;

        AddIfPositiveFromSamples(codes[g++], white);
        AddIfPositiveFromSamples(codes[g++], y1);
        AddIfPositiveFromSamples(codes[g++], y2);
        AddIfPositiveFromSamples(codes[g++], y3);
        AddIfPositiveFromSamples(codes[g++], y4);
        AddIfPositiveFromSamples(codes[g  ], black);

        // FUNNY

        for (var i = 0; i < data.Funny.Length; i++)
        {
            // * D - dark, L - light

            var samples = data.Funny[i];
            if (samples.Count == 0) continue;

            var paleD = samples.Where(x => x.Key is { X: >= X_GRAY and < X_PALE, Y: >= Y_DIM_D and < 50 });
            var paleL = samples.Where(x => x.Key is { X: >= X_GRAY and < X_PALE, Y: >= 50 and < Y_DIM_L });

            var vibrantD = samples.Where(x => x.Key is { X: >= X_PALE, Y: >= Y_VIB_D and < 50 });
            var vibrantL = samples.Where(x => x.Key is { X: >= X_PALE, Y: >= 50 and < Y_VIB_L });

            var veryD = samples.Where(x => x.Key is { X: >= X_GRAY, Y: >= Y_BLACK and < 20 } and not { X: < X_PALE, Y: >= Y_DIM_D });
            var veryL = samples.Where(x => x.Key is { X: >= X_GRAY, Y: >= 80 and < Y_WHITE } and not { X: < X_PALE, Y: <  Y_DIM_L });

            var tones = colorSearchProfile.GetShadesByHue(i);
            var x = 0;

            AddIfPositiveFromSamples(tones[x++], veryL);
            AddIfPositiveFromSamples(tones[x++], vibrantL);
            AddIfPositiveFromSamples(tones[x++], vibrantD);
            AddIfPositiveFromSamples(tones[x++], veryD);
            AddIfPositiveFromSamples(tones[x++], paleD);
            AddIfPositiveFromSamples(tones[x  ], paleL);
        }

        // OTHER

        AddIfPositive(colorSearchProfile.CodeTransparent, () => CalculateTransparencyRank(data.Transparency));

        return result;

        // == FUN ==

        void AddIfPositiveFromSamples(string key, IEnumerable<KeyValuePair<BytePoint, ushort>> samples)
        {
            AddIfPositive(key, () => CalculateRank(samples));
        }

        void AddIfPositive(string key, Func<int> calculateRank)
        {
            var rank = calculateRank();
            if (rank > 0) result.Add(new RankedWord(key, rank));
        }

        int CalculateRank(IEnumerable<KeyValuePair<BytePoint, ushort>> samples)
        {
            var sum = samples.Sum(x => x.Value);
            if (sum <= threshold) return 0;

            var ratio = sum / samplesTotal;
            return (int)Math.Round(ratio * Tag.MAX_RANK);
        }

        int CalculateTransparencyRank(int value)
        {
            if (value == 0) return 0;

            var ratio = value / (samplesTotal * byte.MaxValue);
            return (int)Math.Round(ratio * Tag.MAX_RANK);
        }
    }

    private record ImageScanResult(int TotalSamples, int Transparency, ColorFrequency Grayscale, ColorFrequency[] Funny);
}

internal class ColorFrequency : Dictionary<BytePoint, ushort>
{
    public void AddOrIncrement(BytePoint point)
    {
        if (ContainsKey(point)) this[point]++;
        else /*               */ Add(point, 1);
    }
}

internal readonly struct BytePoint(byte x, byte y)
{
    public byte X { get; } = x;
    public byte Y { get; } = y;

    public override bool Equals(object? obj)
    {
        return obj is BytePoint other
            && X.Equals(other.X)
            && Y.Equals(other.Y);
    }

    public override int GetHashCode()
    {
        return Y << 8 ^ X;
    }
}
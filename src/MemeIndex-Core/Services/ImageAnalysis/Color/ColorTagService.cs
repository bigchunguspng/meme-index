using ColorHelper;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Utils;
using MemeIndex_Core.Utils.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Directory = System.IO.Directory;

namespace MemeIndex_Core.Services.ImageAnalysis.Color;

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

    // SCANNING

    private ImageScanResult ScanImage(Image<Rgba32> image)
    {
        var step = CalculateStep(image.Size);

        var samplesGrayscale = new ColorFrequency();
        var samplesFunny = new ColorFrequencyFunny();

        int totalOpacity = 0, samplesTaken = 0, opaqueSamplesTaken = 0;

        var grayscaleLimits = GetGrayscaleLimits();
        var subPaleLimits = GetSubPaleLimits();
        var paleLimits = GetPaleLimits();

        foreach (var (x, y) in new SizeIterator(image.Size, step))
        {
            samplesTaken++;

            var sample = image[x, y];

            totalOpacity += sample.A;

            if (sample.A < 8) continue; // discard almost invisible pixels

            opaqueSamplesTaken++;

            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var s = hsl.S; // saturation = x
            var l = hsl.L; // lightness  = y

            var hue = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues

#if DEBUG
            image[x, y] = new Rgba32(255, 0, 0);
#endif

            var point = new BytePoint(s, l);

            var sampleTable =
                s < grayscaleLimits[l]
                    ? samplesGrayscale
                    : s < subPaleLimits[l]
                        ? samplesFunny.GetFunny(hue, Shade.WeakPale)
                        : s < paleLimits[l]
                            ? samplesFunny.GetFunny(hue, Shade.StrongPale)
                            : samplesFunny.GetFunny(hue, Shade.Vivid);

            sampleTable.AddOrIncrement(point);
        }

        var transparency = samplesTaken * byte.MaxValue - totalOpacity;

        return new ImageScanResult(samplesTaken, opaqueSamplesTaken, transparency, samplesGrayscale, samplesFunny);
    }

    public static int CalculateStep(Size size)
    {
        var area = size.Width * size.Height;
        var value = Math.Sqrt(area / 4050D).RoundToInt();
        var step = Math.Clamp(value.FloorToEven(), 4, 32);

        Logger.Log($"[step = {step}]");

        return step;
    }

    // LIMITS

    private static readonly BezierLimitBuilder[] _limits =
        Enumerable.Range(0, 4).Select(_ => new BezierLimitBuilder()).ToArray();

    public static int[] GetGrayscaleLimits() => _limits[0].GetLimits(new PointF[]
    {
        new(101, Y_BLACK - 0.5F),
        new(X_GRAY, Y_BLACK), new(X_GRAY, Y_BLACK),
        new(X_GRAY, 50), new(X_GRAY, 50),
        new(X_GRAY, Y_WHITE), new(X_GRAY, Y_WHITE),
        new(101, Y_WHITE + 0.5F)
    });

    public static int[] GetSubPaleLimits() => _limits[1].GetLimits(new PointF[]
    {
        new(101, Y_BLACK + 3.5F),
        new(15, 12), new(15, 12),
        new(10, 50), new(10, 50),
        new(15, 88), new(15, 88),
        new(101, Y_WHITE - 3.5F)
    });

    public static int[] GetPaleLimits() => _limits[2].GetLimits(new PointF[]
    {
        new(101, 20),
        new(40, 20),
        new(20, 40),
        new(20, 50),
        new(20, 50),
        new(20, 60),
        new(40, 80),
        new(101, 80)
    });

    public static int[] GetPeakLimits() => _limits[3].GetLimits(new PointF[]
    {
        new(0, 0 - 1),
        new(0, 10 - 1),
        new(50, 30 - 1),
        new(101, 30 - 1),
        new(101, 70 + 1),
        new(50, 70 + 1),
        new(0, 90 + 1),
        new(0, 100 + 1)
    });

    /*
        0   5       40             100
        +---+--------+---------------+  0
        |              WHITE / BLACK |
        +---+--------+---------------+  4 / 96
        | G |                        |
        | R +--------+  DARK / LIGHT | 12 / 88      <-- [ TO BE DEPRECATED ]
        | A |        |               |
        | Y |        +---------------+ 20 / 80
        |   |  PALE  |               |
        |   |        |    VIBRANT    |
        |   |        |               |
        +---+--------+---------------+ 50
        saturation -->
     */

    // ANALYZING

    private const int X_GRAY = 5;
    private const int Y_BLACK = 04, Y_WHITE = 96;

    private List<RankedWord> AnalyzeImageSamples(ImageScanResult data)
    {
        var samplesTotal = (double)data.TotalSamples;
        var threshold = (int)Math.Round(Math.Log2(samplesTotal / 1000));
        var result = new List<RankedWord>();

        Logger.Log($"[threshold = {threshold}]");

        // GENERAL

        AddIfPositive(colorSearchProfile.CodeTransparent, () => CalculateTransparencyRank(data.Transparency));

        var grayscale = data.Grayscale;
        var funny = data.Funny;

        var funnyTotal = funny.Sum();
        var grayTotal = grayscale.Sum(x => x.Value);

        var grayRangeRatio = grayTotal == 0 ? 0 : (grayscale.Max(x => x.Key.Y) - grayscale.Min(x => x.Key.Y)) / 100D;
        var grayRange = (grayTotal * grayRangeRatio).RoundToInt();

        var totalPaleW = funny.SumByShade(Shade.WeakPale);
        var totalPaleS = funny.SumByShade(Shade.StrongPale);
        var totalVivid = funny.SumByShade(Shade.Vivid);
        var totalD = grayscale.Where(x => x.Key.Y <  50).Sum(x => x.Value) + funny.SumByDarkness(Darkness.Dark);
        var totalL = grayscale.Where(x => x.Key.Y >= 50).Sum(x => x.Value) + funny.SumByDarkness(Darkness.Light);

        Logger.Log(ConsoleColor.Green, $"\tfunnyTotal\t{funnyTotal,7}");
        Logger.Log(ConsoleColor.Green, $"\t grayTotal\t{grayTotal,7}\trange:\t{grayRange} ({grayRangeRatio:P2})");
        Logger.Log(ConsoleColor.Green, $"\ttotalPaleW\t{totalPaleW,7}");
        Logger.Log(ConsoleColor.Green, $"\ttotalPaleS\t{totalPaleS,7}");
        Logger.Log(ConsoleColor.Green, $"\ttotalVivid\t{totalVivid,7}");
        Logger.Log(ConsoleColor.Green, $"\ttotalD    \t{totalD,7}");
        Logger.Log(ConsoleColor.Green, $"\ttotalL    \t{totalL,7}");

        var almostGrayscale = totalPaleW > 8 * totalPaleS && totalPaleS > 8 * totalVivid;
        var paleGrayness = Math.Clamp(Math.Sqrt(totalPaleW / (double)totalPaleS / 40D), 0, 1);
        var grayPart = almostGrayscale ? grayRange + (totalPaleW * paleGrayness).RoundToInt() : grayRange;

        var palePart = almostGrayscale ? totalPaleS : totalPaleS + totalPaleW;
        var paleBase = almostGrayscale ? data.TotalSamples : funnyTotal;

        AddIfPositive("#Y", () => CalculateRankByRatio("#Y", grayPart, data.OpaqueSamples));
        AddIfPositive("#P", () => CalculateRankByRatio("#P", palePart, paleBase));
        AddIfPositive("#S", () => CalculateRankByRatio("#S", totalVivid, funnyTotal));
        AddIfPositive("#D", () => CalculateRankByRatio("#D", totalD, data.OpaqueSamples));
        AddIfPositive("#L", () => CalculateRankByRatio("#L", totalL, data.OpaqueSamples));

        // AddIfPositive("^S", () => ...) // HUE SINGLE     MONOTONE
        // AddIfPositive("^M", () => ...) // HUE MANY       MULTI TONE

        // GRAYSCALE

        var white = grayscale.Where(x => x.Key.Y > Y_WHITE);
        var black = grayscale.Where(x => x.Key.Y < Y_BLACK);

        var y4 = grayscale.Where(x => x.Key is { Y: >= Y_BLACK and < 25 });
        var y3 = grayscale.Where(x => x.Key is { Y: >= 25 and < 50 });
        var y2 = grayscale.Where(x => x.Key is { Y: >= 50 and < 75 });
        var y1 = grayscale.Where(x => x.Key is { Y: >= 75 and < Y_WHITE });

        var codes = colorSearchProfile.ColorsGrayscale.Keys.ToArray();
        var index = 0;

        AddIfPositiveFromSamples(codes[index++], white);
        AddIfPositiveFromSamples(codes[index++], y1);
        AddIfPositiveFromSamples(codes[index++], y2);
        AddIfPositiveFromSamples(codes[index++], y3);
        AddIfPositiveFromSamples(codes[index++], y4);
        AddIfPositiveFromSamples(codes[index  ], black);

        // FUNNY

        // todo treat WEAK-PALE more like GRAYSCALE if #Y is high enough

        var peaks = GetPeakLimits();

        for (var hue = 0; hue < ColorSearchProfile.HUE_COUNT; hue++)
        {
            // * D - dark, L - light

            var vivid = funny.GetFunny(hue, Shade.Vivid);
            var vividD = vivid.Where(x => x.Key.Y <  50);
            var vividL = vivid.Where(x => x.Key.Y >= 50);

            var strongPale = funny.GetFunny(hue, Shade.StrongPale);
            var strongPaleD = strongPale.Where(x => x.Key.Y <  50 && x.Key.X <  peaks[x.Key.Y]);
            var strongPeakD = strongPale.Where(x => x.Key.Y <  50 && x.Key.X >= peaks[x.Key.Y]);
            var strongPaleL = strongPale.Where(x => x.Key.Y >= 50 && x.Key.X <  peaks[x.Key.Y]);
            var strongPeakL = strongPale.Where(x => x.Key.Y >= 50 && x.Key.X >= peaks[x.Key.Y]);

            var weakPale = funny.GetFunny(hue, Shade.WeakPale);
            var weakPaleD = weakPale.Where(x => x.Key.Y <  50 && x.Key.X <  peaks[x.Key.Y]);
            var weakPeakD = weakPale.Where(x => x.Key.Y <  50 && x.Key.X >= peaks[x.Key.Y]);
            var weakPaleL = weakPale.Where(x => x.Key.Y >= 50 && x.Key.X <  peaks[x.Key.Y]);
            var weakPeakL = weakPale.Where(x => x.Key.Y >= 50 && x.Key.X >= peaks[x.Key.Y]);

            var shades = colorSearchProfile.GetShadesByHue(hue);
            var x = 0;

            AddIfPositive(shades[x++], () => CalculateRankPale(weakPeakL, strongPeakL));
            AddIfPositive(shades[x++], () => CalculateRank(vividL));
            AddIfPositive(shades[x++], () => CalculateRank(vividD));
            AddIfPositive(shades[x++], () => CalculateRankPale(weakPeakD, strongPeakD));
            AddIfPositive(shades[x++], () => CalculateRankPale(weakPaleD, strongPaleD));
            AddIfPositive(shades[x  ], () => CalculateRankPale(weakPaleL, strongPaleL));
        }

        // OTHER

        var dominantColor = result
            .Where(x => x.Rank >= 2000)
            .OrderByDescending(x => x.Rank)
            .FirstOrDefault(x => !x.Word.StartsWith('#'));
        if (dominantColor is not null)
        {
            var ratio = (dominantColor.Rank - 1984) / (double)(Tag.MAX_RANK - 1984);
            AddIfPositive("#A", () => (ratio * Tag.MAX_RANK).RoundToInt());
        }

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

        int CalculateRankPale
        (
            IEnumerable<KeyValuePair<BytePoint, ushort>> samplesW,
            IEnumerable<KeyValuePair<BytePoint, ushort>> samplesS
        )
        {
            var sumS = samplesS.Sum(x => x.Value);
            var sumW = samplesW.Sum(x => x.Value);
            if (sumS <= threshold && sumW <= threshold * 4) return 0;

            var ratio = (sumS + sumW) / samplesTotal;
            return (int)Math.Round(ratio * Tag.MAX_RANK);
        }

        int CalculateTransparencyRank(int value)
        {
            if (value == 0) return 0;

            var ratio = value / (samplesTotal * byte.MaxValue);
            return (int)Math.Round(ratio * Tag.MAX_RANK);
        }

        int CalculateRankByRatio(string key, int value, int total)
        {
            if (value == 0 || total == 0) return 0;

            var ratio = value / (double)total;
            var color = ratio < 0.25 ? ConsoleColor.Red : ConsoleColor.Yellow;
            Logger.Log(color, $"\t\t{key}\t{ratio:P2}");
            return ratio < 0.25 ? 0 : (Math.Pow(0.0001, 1 - ratio) * Tag.MAX_RANK).RoundToInt();

            // 100% -> 10_000, 75% -> 1000, 50% -> 100, 25% -> 10
        }
    }

    private record ImageScanResult
    (
        int TotalSamples,
        int OpaqueSamples,
        int Transparency,
        ColorFrequency Grayscale,
        ColorFrequencyFunny Funny
    );
}

internal class ColorFrequencyFunny
{
    private readonly ColorFrequency[] _samples = GetEmptyTable();

    private static ColorFrequency[] GetEmptyTable()
    {
        const int length = ColorSearchProfile.HUE_COUNT * 3;
        return Enumerable.Range(0, length).Select(_ => new ColorFrequency()).ToArray();
    }

    public ColorFrequency GetFunny(int hue, Shade shade)
    {
        return _samples[ColorSearchProfile.HUE_COUNT * (int)shade + hue];
    }

    public int SumByShade(Shade shade) => _samples
        .Skip(ColorSearchProfile.HUE_COUNT * (int)shade)
        .Take(ColorSearchProfile.HUE_COUNT)
        .Sum(samples => samples.Sum(x => x.Value));

    public int SumByDarkness(Darkness darkness) => _samples
        .Sum(samples =>
        {
            return samples
                .Where(x => darkness == Darkness.Dark == x.Key.Y < 50)
                .Sum(x => x.Value);
        });

    public int Sum() => _samples.Sum(samples => samples.Sum(x => x.Value));
}

internal enum Shade
{
    WeakPale,
    StrongPale,
    Vivid
}

internal enum Darkness
{
    Dark,
    Light
}

internal class ColorFrequency : Dictionary<BytePoint, ushort>
{
    public void AddOrIncrement(BytePoint point)
    {
        if (ContainsKey(point)) this[point]++;
        else /*               */ Add(point, 1);
    }
}
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

            // TODO BETTER GRAYSCALE / RGB24 HANDLING

            var sw = Helpers.GetStartedStopwatch();

            using var image = await Image.LoadAsync<Rgba32>(path);

            sw.Log("IMAGE LOADED");

            // 720 x 720 => 16
            // 180 x 180 =>  4
            var step = (int)Math.Clamp(Math.Sqrt(image.Width * image.Height / 2025D), 2, 32);
            Logger.Log($"[step = {step}]");

            var samplesGrayscale = new ColorFrequency();
            var samplesFunny = colorSearchProfile.Hues.Select(_ => new ColorFrequency()).ToArray();

            sw.Restart();

            // SCAN IMAGE
            var samplesCollected = 0;
            for (var x = 0; x < image.Width;  x += step)
            for (var y = 0; y < image.Height; y += step)
            {
                var sample = image[x, y];

                var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
                var s = hsl.S;
                var l = hsl.L;

                var hue = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues

#if DEBUG
                image[x, y] = Color.Red;
#endif

                var point = new Point(s, l);

                var grayscale = s < 10 || l is < 4 or > 96;
                var sampleTable = grayscale ? samplesGrayscale : samplesFunny[hue];

                sampleTable.ContainsKey(point).Execute
                (
                    () => sampleTable[point]++,
                    () => sampleTable.Add(point, 1)
                );

                samplesCollected++;
            }

            sw.Log($"IMAGE SCANNED ({samplesCollected} samples collected)");

#if DEBUG
            var ticks = DateTime.UtcNow.Ticks;
            var name = $"baka-{path.FancyHashCode()}-{ticks >> 32}";
            Directory.CreateDirectory("img");
            await image.SaveAsJpegAsync(Path.Combine("img", $"{name}-dots.jpg"), _defaultJpegEncoder);

            sw.Log("DEBUG-DOTS EXPORTED");
#endif

            // ANALYZE

            var samplesTotal = (double)samplesCollected;
            var threshold = (int)Math.Round(Math.Log2(samplesTotal / 1000));
            var result = new List<RankedWord>();

            Logger.Log($"[threshold = {threshold}]");

            var white = samplesGrayscale.Where(x => x.Key.Y > 96);
            var black = samplesGrayscale.Where(x => x.Key.Y < 04);

            var gray = samplesGrayscale.Where(x => x.Key is { X: < 10, Y: >= 4 and <= 96 }).ToList();

            var y4 = gray.Where(x => x.Key is { Y:           < 27 });
            var y3 = gray.Where(x => x.Key is { Y: >= 27 and < 50 });
            var y2 = gray.Where(x => x.Key is { Y: >= 50 and < 73 });
            var y1 = gray.Where(x => x.Key is { Y: >= 73          });

            var codes = colorSearchProfile.ColorsGrayscale.Keys.ToArray();
            var g = 0;

            AddIfPositive(codes[g++], white);
            AddIfPositive(codes[g++], y1);
            AddIfPositive(codes[g++], y2);
            AddIfPositive(codes[g++], y3);
            AddIfPositive(codes[g++], y4);
            AddIfPositive(codes[g  ], black);

            for (var i = 0; i < samplesFunny.Length; i++)
            {
                // * D - dark, L - light

                var samples = samplesFunny[i];
                if (samples.Count == 0) continue;

                var paleD = samples.Where(x => x.Key is { X: >= 10 and < 40, Y: >= 12 and < 50 });
                var paleL = samples.Where(x => x.Key is { X: >= 10 and < 40, Y: >= 50 and < 88 });

                var vibrantD = samples.Where(x => x.Key is { X: >= 40, Y: >= 20 and < 50 });
                var vibrantL = samples.Where(x => x.Key is { X: >= 40, Y: >= 50 and < 80 });

                var veryD = samples.Where(x => x.Key is { X: >= 10, Y: >= 04 and < 20 } and not { X: < 40, Y: >= 12 });
                var veryL = samples.Where(x => x.Key is { X: >= 10, Y: >= 80 and < 96 } and not { X: < 40, Y: <  88 });

                var tones = colorSearchProfile.GetShadesByHue(i);
                var x = 0;

                AddIfPositive(tones[x++], veryL);
                AddIfPositive(tones[x++], vibrantL);
                AddIfPositive(tones[x++], vibrantD);
                AddIfPositive(tones[x++], veryD);
                AddIfPositive(tones[x++], paleD);
                AddIfPositive(tones[x  ], paleL);
            }

            sw.Log("ANALYSIS DONE");

            // RETURN

            return result.OrderByDescending(x => x.Rank).ToList();


            // == FUN ==

            void AddIfPositive(string key, IEnumerable<KeyValuePair<Point, ushort>> samples)
            {
                var rank = CalculateRank(samples);
                if (rank > 0) result.Add(new RankedWord(key, rank));
            }

            int CalculateRank(IEnumerable<KeyValuePair<Point, ushort>> samples)
            {
                var sum = samples.Sum(x => x.Value);
                if (sum <= threshold) return 0;

                var ratio = sum / samplesTotal;
                return (int)Math.Round(ratio * Tag.MAX_RANK);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(ColorTagService), e);
            return null;
        }
    }
}

internal class ColorFrequency : Dictionary<Point, ushort>;
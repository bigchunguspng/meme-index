using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using FrequencySample = System.Collections.Generic.KeyValuePair<MemeIndex.Tools.Geometry.BytePoint, ushort>;

namespace MemeIndex.Core.Analysis.Color.v1;

public static class ColorTagService // todo rename
{
    public const int MAX_RANK = 10_000;

    // todo text -> *better term*
    public static async Task<List<RankedWord>?> GetTextRepresentation(FilePath path)
    {
        try
        {
            // CHECK FILE
            var file = new FileInfo(path);
            if (file.Exists == false)
                return null;

            var sw = Stopwatch.StartNew();

            using var image = await Image.LoadAsync<Rgba32>(path);
            sw.Log("IMAGE LOADED");

            var scanResult = ScanImage(image);
            sw.Log($"IMAGE SCANNED ({scanResult.TotalSamples} samples collected)");
#if DEBUG
            var jpeg = Dir_Debug_SampleGrids
                .EnsureDirectoryExist()
                .Combine($"Dots-{DateTime.UtcNow.Ticks >> 32}-{Desert.GetSand(4)}.jpg");
            await image.SaveAsJpegAsync(jpeg, DebugTools.JpegEncoder_Q80);
            sw.Log("DEBUG-DOTS   EXPORTED");
            DebugTools.RenderSamplePoster(path);
            sw.Log("DEBUG-POSTER EXPORTED");
#endif
            var rankedWords = AnalyzeImageSamples(scanResult);
            sw.Log("ANALYSIS DONE");

            rankedWords.Sort((w1, w2) => w2.Rank - w1.Rank); // DESC
            return rankedWords;
        }
        catch (Exception e)
        {
            LogError(e);
            return null;
        }
    }

    // SCANNING

    private record ImageScanResult
    (
        int TotalSamples,
        int OpaqueSamples,
        int Transparency,
        ColorFrequency Grayscale,
        ColorFrequencyFunny Funny
    );

    private static ImageScanResult ScanImage(Image<Rgba32> image)
    {
        var step = CalculateStep(image.Size);
        Log($"[step = {step}]");

        var samplesGrayscale = new ColorFrequency();
        var samplesFunny = new ColorFrequencyFunny();

        int totalOpacity = 0, samplesTaken = 0, opaqueSamplesTaken = 0;

        var grayscaleLimits = GetGrayscaleLimits();
        var   subPaleLimits =   GetSubPaleLimits();
        var      paleLimits =      GetPaleLimits();

        foreach (var (x, y) in new SizeIterator_45deg(image.Size, step))
        {
            var sample = image[x, y];

            samplesTaken++;
            totalOpacity += sample.A;

            if (sample.A < 8) continue; // discard almost invisible pixels

            opaqueSamplesTaken++;

            var hsl = sample.Rgb.ToHsl();
            var s = hsl.S; // saturation = x
            var l = hsl.L; // lightness  = y

            var hue_index = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues
#if DEBUG
            image[x, y] = new Rgba32(255, 0, 0); // mark sample coords
#endif
            var sampleTable
                = s < grayscaleLimits[l] ? samplesGrayscale
                : s <   subPaleLimits[l] ? samplesFunny.GetFunny(hue_index, Shade.WeakPale)
                : s <      paleLimits[l] ? samplesFunny.GetFunny(hue_index, Shade.StrongPale)
                :                          samplesFunny.GetFunny(hue_index, Shade.Vivid);

            sampleTable.AddOrIncrement(new BytePoint(s, l));
        }

        var transparency = samplesTaken * byte.MaxValue - totalOpacity;

        return new ImageScanResult(samplesTaken, opaqueSamplesTaken, transparency, samplesGrayscale, samplesFunny);
    }

    /// Result: 4..32, even number. Which gives 5-10k samples per image.
    public static int CalculateStep(Size size)
    {
        var area = size.Width * size.Height;
        var v1 = Math.Sqrt(area / 4000.0);
        var v2 = v1.RoundInt().EvenFloor();
        return Math.Clamp(v2, 4, 32);
    }

    // LIMITS

    private const int
        X_GRAY  =  5,
        Y_BLACK =  4,
        Y_WHITE = 96;

    private static readonly BezierLimitBuilder[] _limits
        = 4.Times(() => new BezierLimitBuilder());

    public static int[] GetGrayscaleLimits() => _limits[0].GetLimits(GrayscaleLimits);
    public static int[]   GetSubPaleLimits() => _limits[1].GetLimits  (SubPaleLimits);
    public static int[]      GetPaleLimits() => _limits[2].GetLimits     (PaleLimits);
    public static int[]      GetPeakLimits() => _limits[3].GetLimits     (PeakLimits);

    private static readonly PointF[]
        GrayscaleLimits =
        [
            new(101, Y_BLACK - 0.5F),
            new(X_GRAY, Y_BLACK), new(X_GRAY, Y_BLACK),
            new(X_GRAY,      50), new(X_GRAY,      50),
            new(X_GRAY, Y_WHITE), new(X_GRAY, Y_WHITE),
            new(101, Y_WHITE + 0.5F),
        ],
        SubPaleLimits =
        [
            new(101, Y_BLACK + 3.5F),
            new( 15, 12), new(15, 12),
            new( 10, 50), new(10, 50),
            new( 15, 88), new(15, 88),
            new(101, Y_WHITE - 3.5F),
        ],
        PaleLimits =
        [
            new(101, 20),
            new( 40, 20),
            new( 20, 40),
            new( 20, 50),
            new( 20, 50),
            new( 20, 60),
            new( 40, 80),
            new(101, 80),
        ],
        PeakLimits =
        [
            new(  0,   0 - 1),
            new(  0,  10 - 1),
            new( 50,  30 - 1),
            new(101,  30 - 1),
            new(101,  70 + 1),
            new( 50,  70 + 1),
            new(  0,  90 + 1),
            new(  0, 100 + 1),
        ];

    // ANALYZING

    private static List<RankedWord> AnalyzeImageSamples(ImageScanResult data)
    {
        var samplesTotal = (double)data.TotalSamples;
        var threshold = (int)Math.Round(Math.Log2(samplesTotal / 1000));
        var result = new List<RankedWord>();

        Log($"[threshold = {threshold}]");

        // GENERAL

        AddIfPositive(ColorSearchProfile.CodeTransparent, () => CalculateTransparencyRank(data.Transparency));

        var grayscale = data.Grayscale;
        var funny     = data.Funny;

        var  grayTotal = grayscale.Sum(x => x.Value);
        var funnyTotal =     funny.Sum();

        var grayRangeRatio = grayTotal == 0 ? 0 : (grayscale.Max(x => x.Key.Y) - grayscale.Min(x => x.Key.Y)) / 100.0;
        var grayRange = (grayTotal * grayRangeRatio).RoundInt();

        var totalPaleW = funny.SumByShade(Shade.WeakPale);
        var totalPaleS = funny.SumByShade(Shade.StrongPale);
        var totalVivid = funny.SumByShade(Shade.Vivid);
        var totalD = grayscale.Where(x => x.Key.Y <  50).Sum(x => x.Value) + funny.SumByDarkness(Darkness.Dark);
        var totalL = grayscale.Where(x => x.Key.Y >= 50).Sum(x => x.Value) + funny.SumByDarkness(Darkness.Light);

        LogCM(ConsoleColor.Green, $"\tfunnyTotal\t{funnyTotal,7}");
        LogCM(ConsoleColor.Green, $"\t grayTotal\t{grayTotal,7}\trange:\t{grayRange} ({grayRangeRatio:P2})");
        LogCM(ConsoleColor.Green, $"\ttotalPaleW\t{totalPaleW,7}");
        LogCM(ConsoleColor.Green, $"\ttotalPaleS\t{totalPaleS,7}");
        LogCM(ConsoleColor.Green, $"\ttotalVivid\t{totalVivid,7}");
        LogCM(ConsoleColor.Green, $"\ttotalD    \t{totalD,7}");
        LogCM(ConsoleColor.Green, $"\ttotalL    \t{totalL,7}");

        var almostGrayscale = totalPaleW > 8 * totalPaleS && totalPaleS > 8 * totalVivid;
        var paleGrayness = Math.Clamp(Math.Sqrt(totalPaleW / (double)totalPaleS / 40.0), 0, 1);
        var grayPart = almostGrayscale ? grayRange + (totalPaleW * paleGrayness).RoundInt() : grayRange;

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

        var codes = ColorSearchProfile.ColorsGrayscale.Keys.ToArray();

        AddIfPositiveFromSamples(codes[0], white);
        AddIfPositiveFromSamples(codes[1], y1);
        AddIfPositiveFromSamples(codes[2], y2);
        AddIfPositiveFromSamples(codes[3], y3);
        AddIfPositiveFromSamples(codes[4], y4);
        AddIfPositiveFromSamples(codes[5], black);

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

            var shades = ColorSearchProfile.GetShadesByHue(hue);

            AddIfPositive(shades[0], () => CalculateRankPale(weakPeakL, strongPeakL));
            AddIfPositive(shades[1], () => CalculateRank(vividL));
            AddIfPositive(shades[2], () => CalculateRank(vividD));
            AddIfPositive(shades[3], () => CalculateRankPale(weakPeakD, strongPeakD));
            AddIfPositive(shades[4], () => CalculateRankPale(weakPaleD, strongPaleD));
            AddIfPositive(shades[5], () => CalculateRankPale(weakPaleL, strongPaleL));
        }

        // OTHER

        var dominantColor = result
            .Where(x => x.Rank >= 2000)
            .OrderByDescending(x => x.Rank)
            .FirstOrDefault(x => !x.Word.StartsWith('#'));
        if (dominantColor is not null)
        {
            var ratio = (dominantColor.Rank - 1984) / (double)(MAX_RANK - 1984);
            AddIfPositive("#A", () => (ratio * MAX_RANK).RoundInt());
        }

        return result;

        // == FUN ==

        void AddIfPositiveFromSamples(string key, IEnumerable<FrequencySample> samples)
        {
            AddIfPositive(key, () => CalculateRank(samples));
        }

        void AddIfPositive(string key, Func<int> calculateRank)
        {
            var rank = calculateRank();
            if (rank > 0) result.Add(new RankedWord(key, rank));
        }

        int CalculateRank(IEnumerable<FrequencySample> samples)
        {
            var sum = samples.Sum(x => x.Value);
            if (sum <= threshold) return 0;

            var ratio = sum / samplesTotal;
            return (int)Math.Round(ratio * MAX_RANK);
        }

        int CalculateRankPale
        (
            IEnumerable<FrequencySample> samplesW,
            IEnumerable<FrequencySample> samplesS
        )
        {
            var sumS = samplesS.Sum(x => x.Value);
            var sumW = samplesW.Sum(x => x.Value);
            if (sumS <= threshold && sumW <= threshold * 4) return 0;

            var ratio = (sumS + sumW) / samplesTotal;
            return (int)Math.Round(ratio * MAX_RANK);
        }

        int CalculateTransparencyRank(int value)
        {
            if (value == 0) return 0;

            var ratio = value / (samplesTotal * byte.MaxValue);
            return (int)Math.Round(ratio * MAX_RANK);
        }

        int CalculateRankByRatio(string key, int value, int total)
        {
            if (value == 0 || total == 0) return 0;

            var ratio = value / (double)total;
            var color = ratio < 0.25 ? ConsoleColor.Red : ConsoleColor.Yellow;
            Log($"\t\t{key}\t{ratio:P2}", color: color);
            return ratio < 0.25 ? 0 : (Math.Pow(0.0001, 1 - ratio) * MAX_RANK).RoundInt();

            // 100% -> 10_000, 75% -> 1000, 50% -> 100, 25% -> 10
        }
    }
}

/// Square 256×256 px where some pixels hold frequency value (0-64k).
internal class ColorFrequency : Dictionary<BytePoint, ushort>
{
    public void AddOrIncrement(BytePoint point)
    {
        if (ContainsKey(point)) this[point]++; // todo handle overflow
        else /*               */ Add(point, 1);
    }
}

internal class ColorFrequencyFunny
{
    /// 3 rows - shades, 12 columns - hues.
    private readonly ColorFrequency[] _samples_byShadeAndHue =
        (3 * ColorSearchProfile.HUE_COUNT).Times(() => new ColorFrequency());

    public ColorFrequency GetFunny(int hue_index, Shade shade)
    {
        var row = ColorSearchProfile.HUE_COUNT * (int)shade;
        return _samples_byShadeAndHue[row + hue_index];
    }

    public int SumByShade(Shade shade)
        => _samples_byShadeAndHue
            .Skip(ColorSearchProfile.HUE_COUNT * (int)shade)
            .Take(ColorSearchProfile.HUE_COUNT)
            .Sum(samples => samples.Sum(x => x.Value));

    public int SumByDarkness(Darkness darkness)
        => _samples_byShadeAndHue
            .Sum(samples => samples
                .Where(x => darkness == Darkness.Dark == x.Key.Y < 50)
                .Sum(x => x.Value));

    public int Sum()
        => _samples_byShadeAndHue
            .Sum(samples => samples.Sum(x => x.Value));
}

internal enum Shade
{
    WeakPale,
    StrongPale,
    Vivid,
}

internal enum Darkness
{
    Dark,
    Light,
}
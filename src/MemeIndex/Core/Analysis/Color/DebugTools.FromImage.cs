using ColorHelper;
using MemeIndex.Core.Analysis.Color.v1;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color;

public static partial class DebugTools
{
    // PROFILE

    public static void RenderProfile_Oklch_HxL
        (string path) => RenderProfile(path, GetReportBackground_HxL, "Oklch-HxL", (report, sample) =>
    {
        var oklch = sample.ToOklch();
        var ly = (oklch.L * 100).RoundInt().Clamp(0, 100);
        var hx = (oklch.H.RoundInt() % 360).Clamp(0, 360);

        report[hx, ly] = sample;
    });

    public static void RenderProfile_Oklch_v2
        (string path) => RenderProfile(path, () => GetReportBackground_Oklch(useMagenta: false), "Oklch-v2", (report, sample) =>
    {
        var oklch = sample.ToOklch();
        var cy = 100 - (oklch.C * 300).RoundInt().Clamp(0, 100); // ↑
        var lx =       (oklch.L * 100).RoundInt().Clamp(0, 100); //    →

        Span<int> hue_ixs = stackalloc int [2];
        ColorAnalyzer_v2.GetHueIndices(oklch, ref hue_ixs);

        foreach (var hue_ix in hue_ixs)
        {
            if (hue_ix == -1) continue;

            var row = hue_ix  & 1; // 0 2 | 4 6 | 8
            var col = hue_ix >> 2; // 1 3 | 5 7 | 9

            report[col * SIDE + lx, row * SIDE + cy] = sample;
        }
    });

    public static void RenderProfile_Oklch
        (string path) => RenderProfile(path, () => GetReportBackground_Oklch(), "Oklch", (report, sample) =>
    {
        var oklch = sample.ToOklch();
        var cy = 100 - (oklch.C * 300).RoundInt().Clamp(0, 100); // ↑
        var lx =       (oklch.L * 100).RoundInt().Clamp(0, 100); //    →

        var hue_ix = ColorProfile.GetHueIndex(oklch.H);

        var row = hue_ix  & 1; // 0 2 | 4 6 | 8 A
        var col = hue_ix >> 2; // 1 3 | 5 7 | 9

        report[col * SIDE + lx, row * SIDE + cy] = sample;
    });

    public static void RenderProfile_HSL
        (string path) => RenderProfile(path, GetReportBackground_HSL, "HSL", (report, sample) =>
    {
        var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
        var l = hsl.L;
        var s = hsl.S;

        var hue_ix = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues
        var offsetX = hue_ix / 4 * SIDE;
        var offsetY = hue_ix % 2 == 0 ? 0 : SIDE;

        report[offsetX + s, offsetY + l] = sample;
    });

    private static void RenderProfile
    (
        string path,
        Func<Image<Rgb24>> getReportBg,
        string suffix,
        Action<Image<Rgb24>, Rgb24> useSample
    )
    {
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("1. Load image.");

        using var report = getReportBg();
        sw.Log("2. Draw report background.");

        var step = ColorTagService.CalculateStep(source.Size);
        var samplesTotal = 0;

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];
            samplesTotal++;

            useSample(report, sample);
        }
        sw.Log("3. Collect samples.");

        var name = $"Profile-{DateTime.UtcNow.Ticks:x16}-{Desert.GetSand()}-{suffix}.png";
        var save = Dir_Debug_Profiles.EnsureDirectoryExist().Combine(name);
        report.SaveAsPng(save);
        sw.Log($"4. Save report >> \"{name}\"");

        Log($"[step: {step,3}, samples collected: {samplesTotal,6}]");
    }

    // POSTER

    public static void RenderSamplePoster(string path)
    {
        using var source = Image.Load<Rgb24>(path);

        var step = ColorTagService.CalculateStep(source.Size);
        var halfStep = step / 2;

        var w = source.Width;
        var h = source.Height;
        var image_actual = new Image<Rgb24>(w, h);
        var image_poster = new Image<Rgb24>(w, h);

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            var oklch = sample.ToOklch();
            var hue_ix = ColorProfile.GetHueIndex(oklch.H);
            var saturated = ColorProfile.IsSaturated(oklch);
            var bucket = saturated
                ? ColorProfile.GetBucketIndex_Saturated(oklch)
                : ColorProfile.GetBucketIndex_Grayscale(oklch);
            var color = saturated
                ? ColorProfile.GetColor_Saturated(hue_ix, bucket)
                : ColorProfile.GetColor_Grayscale(bucket);

            for (var y0 = y - halfStep; y0 < y + halfStep; y0++)
            for (var x0 = x - halfStep; x0 < x + halfStep; x0++)
            {
                if (x0 < 0 || y0 < 0 || x0 >= w || y0 >= h)
                    continue;

                var xd = Math.Abs(x - x0);
                var yd = Math.Abs(y - y0);
                if (xd + yd > halfStep)
                    continue;

                image_actual[x0, y0] = sample;
                image_poster[x0, y0] = color;
            }
        }

        var ticks = DateTime.UtcNow.Ticks;
        var sand = Desert.GetSand();
        var jpeg1 = Dir_Debug_Image
            .EnsureDirectoryExist()
            .Combine($"Samples-{ticks}-{sand}.jpg");
        var jpeg2 = Dir_Debug_Image
            .Combine($"Poster-{ticks}-{sand}.jpg");
        //image_actual.SaveAsJpeg(jpeg1, JpegEncoder_Q80);
        image_poster.SaveAsJpeg(jpeg2, JpegEncoder_Q80);
    }
}
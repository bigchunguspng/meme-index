using ColorHelper;
using MemeIndex.Core.Analysis.Color.v2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex.Core.Analysis.Color;

public static partial class DebugTools
{
    public static void HSL(int step)
    {
        using var image = new Image<Rgb24>(100, 100);

        for (var hue = 0; hue < 360; hue += step)
        {
            for (var x = 0; x < image.Width; x++)
            for (var y = 0; y < image.Height; y++)
            {
                var hsl = new HSL(hue, (byte)x, (byte)y);
                var rgb = hsl.ToRgb24();
                var color = new Rgb24(rgb.R, rgb.G, rgb.B);
                image[x, y] = color;
            }

            var path = Dir_Debug_Color
                .EnsureDirectoryExist()
                .Combine($"{nameof(HSL)}-{hue}.png");
            image.SaveAsPng(path);
        }
    }

    public static void Oklch(int step)
    {
        using var image = new Image<Rgb24>(100, 100);

        for (var hue = 0; hue < 360; hue += step)
        {
            for (var x = 0; x < image.Width; x++)
            for (var y = 0; y < image.Height; y++)
            {
                var oklch = new Oklch(x / 100.0, y / 100.0, hue);
                var color = oklch.ToRgb24();
                image[x, y] = color;
            }

            var path = Dir_Debug_Color
                .EnsureDirectoryExist()
                .Combine($"{nameof(Oklch)}-{hue}.png");
            image.SaveAsPng(path);
        }
    }

    public static void Oklch_HxL(double step = 0.05)
    {
        using var image = new Image<Rgb24>(360, 100);

        for (var chroma = 0.0; chroma < 1.0; chroma += step)
        {
            for (var l = 0; l < image.Height; l++)
            for (var h = 0; h < image.Width; h++)
            {
                var hsl = new HSL(h, (byte)(chroma * 100).RoundInt(), (byte)l);
                var rgb = hsl.ToRgb24();
                //var oklch = new Oklch(l / 100.0, chroma, h);
                var oklch = rgb.ToOklch();
                var x =  oklch.H       .RoundInt().Clamp(0, 360 - 1);
                var y = (oklch.L * 100).RoundInt().Clamp(0, 100 - 1);
                var color = oklch.ToRgb24();
                image[x, y] = color;
            }

            var path = Dir_Debug_Color
                .EnsureDirectoryExist()
                .Combine($"{nameof(Oklch_HxL)}-HxL-2-{chroma:F2}.png");
            image.SaveAsPng(path);
        }
    }

    //

    public static void RenderHues_Oklch_v2()
    {
        var images = ColorAnalyzer_v2.N_HUES.Times(GetReportBackground_HxL);
        var ticks = $"{DateTime.UtcNow.Ticks >> 24:x}";

        Span<int> hue_ixs = stackalloc int[2];
        for (byte s = 0; s < 100; s++)
        for (var  h = 0; h < 360; h++)
        for (byte l = 0; l < 100; l++)
        {
            var hsl = new HSL(h, s, l);
            var rgb = hsl.ToRgb24();
            var ok  = rgb.ToOklch();

            ColorAnalyzer_v2.GetHueIndices(ok, ref hue_ixs);
            foreach (var hue_ix in hue_ixs)
            {
                if (hue_ix == -1) continue;

                images[hue_ix][ok.IntH, ok.IntL] = rgb;
            }
        }

        for (var i = 0; i < ColorAnalyzer_v2.N_HUES; i++)
        {
            var path = Dir_Debug_Color.EnsureDirectoryExist().Combine($"Oklch-v2-Hue-{ticks}-{i:00}.png");
            images[i].SaveAsPng(path); 
        }
    }

    public static void RenderFullOklch_On3x2()
    {
        var report = GetReportBackground_Oklch();
        var oklchs = GetOklchCube_SortedBy_HCL();

        for (var i = 0; i < 256 * 256 * 256; i++)
        {
            var color = oklchs[i];
            var hi = color.IntH.Cap(359) / 60;
            var col = hi % 3;
            var row = hi / 3;
            var lx =       color.IntL;
            var cy = 100 - color.IntC;
            report[SIDE * col + lx, SIDE * row + cy] = color.ToRgb24();
        }
        Log("loop OKLCH");

        var path = Dir_Debug_Color.EnsureDirectoryExist().Combine($"Oklch-full-smooth-{Desert.GetSand()}.png");
        report.SaveAsPng(path);
        Log("SaveAsPng");
    }

    public static void RenderFullOklch_Frames()
    {
        var oklchs = GetOklchCube_SortedBy_HCL();
        var dir = Dir_Debug_Color
            .Combine($"Frames-{Desert.GetSand()}")
            .EnsureDirectoryExist();

        var N = 256 * 256 * 256;
        var frame = new Image<Rgb24>(102, 102, 50.ToRgb24());
        var fr_curr = -1;
        for (var i = 0; i < N; i++)
        {
            var color = oklchs[i];
            var fr =       color.IntH;
            var lx =       color.IntL;
            var cy = 100 - color.IntC;
            if (fr != fr_curr)
            {
                SaveFrame(fr);
            }
            frame[lx, cy] = color.ToRgb24();
        }
        SaveFrame(fr_curr);

        void SaveFrame(int fr)
        {
            var path = dir.Combine($"O-{fr:000}.png");
            frame.SaveAsPng(path);
            frame.Mutate(x => x.Fill(50.ToRgb24()));
            fr_curr = fr;
        }
    }

    private static Oklch[] GetOklchCube_SortedBy_HCL()
    {
        var oklchs = new Oklch[256 * 256 * 256]; // 16M * 24B = 284MB
        var i = 0;
        for (var r = 0; r < 256; r++)
        for (var g = 0; g < 256; g++)
        for (var b = 0; b < 256; b++)
        {
            oklchs[i++] = new Rgb24((byte)r, (byte)g, (byte)b).ToOklch();
        }
        Log("loop RGB");
        
        Array.Sort(oklchs, (a, b) => 10_000 * (a.IntH - b.IntH) + 100 * (a.IntC - b.IntC) + (a.IntL - b.IntL));
        Log("Array.Sort");

        return oklchs;
    }

    public static void CompareHLS_ToOklch()
    {
        using var report_1 = new Image<Rgb24>(360, 101, new Rgb24(50, 50, 50));
        using var report_2 = new Image<Rgb24>(360, 101, new Rgb24(50, 50, 50));

        var color = 40.ToRgb24();
        for (var r_hi = 0; r_hi < 12; r_hi += 2)
        {
            var rect = new RectangleF(r_hi * 30, 0, 30, 360);
            report_1.Mutate(x => x.Fill(color, rect));
            report_2.Mutate(x => x.Fill(color, rect));
        }

        for (byte s = 0; s < 100; s++)
        for (var  h = 0; h < 360; h++)
        for (byte l = 0; l < 100; l++)
        {
            var hsl = new HSL(h, s, l);
            var rgb = ColorConverter.HslToRgb(hsl).ToRgb24();
            var HSL = ColorConverter.RgbToHsl(rgb.ToRGB());
            var OkLCH = rgb.ToOklch();
            //if (Math.Abs(hsl.L - HSL.L) > 5) LogError($"{hsl.H},{hsl.S},{hsl.L} != {HSL.H},{HSL.S},{HSL.L}");
            report_1[HSL.H.Clamp(0, 360 - 1), HSL.L] = rgb;
            report_2[OkLCH.H.RoundInt().Clamp(0, 360 - 1), (OkLCH.L * 100).RoundInt().Clamp(0, 100)] = rgb;
        }

        var name_1 = $"Test-{DateTime.UtcNow.Ticks}-H-Full.png";
        var name_2 = $"Test-{DateTime.UtcNow.Ticks}-O-Full.png";
        var save_1 = Dir_Debug_Color
            .EnsureDirectoryExist()
            .Combine(name_1);
        var save_2 = Dir_Debug_Color
            .Combine(name_2);

        report_1.SaveAsPng(save_1);
        report_2.SaveAsPng(save_2);
    }

    public static void RenderHueReferences_Frames()
    {
        var dir = Dir_Debug_Color.Combine($"Frames-HR-{Desert.GetSand()}").EnsureDirectoryExist();

        var report = GetReportBackground_Oklch();
        var frame = new Image<Rgb24>(SIDE, SIDE, 50.ToRgb24());
        frame.Mutate(x => x.DrawImage(report, new Point(0, 0), 1));
        var refs = ColorAnalyzer_v2.HueReferences;
        for (var h = 0; h < refs.Length; h++)
        {
            for (var o = 0; o < 6; o++)
            {
                var oklch = refs[h][o];
                var x = oklch.IntL;
                var y = oklch.IntC;
                var rgb = oklch.ToRgb24();
                frame[x, 100 - y] = rgb;
            }

            var path = dir.Combine($"O-{h:000}.png");
            frame.SaveAsPng(path);
            frame.Mutate(x => x.DrawImage(report, new Point(0, 0), 1));
        }
    }
}
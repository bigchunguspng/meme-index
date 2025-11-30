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
}
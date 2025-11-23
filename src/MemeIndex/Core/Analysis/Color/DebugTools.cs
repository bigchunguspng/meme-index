using ColorHelper;
using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex.Core.Analysis.Color;

public static class DebugTools
{
    private const int SIDE = 101;

    public static readonly JpegEncoder JpegEncoder_Q80 = new() { Quality = 80 };

    public static void LoopHSL(int step)
    {
        for (var hue = 0; hue < 360; hue += step)
        {
            HSL(hue);
        }
    }

    public static void HSL(int hue)
    {
        var sw = Stopwatch.StartNew();

        using var image = new Image<Rgb24>(100, 100);

        for (var x = 0; x < image.Width; x++)
        for (var y = 0; y < image.Height; y++)
        {
            var hsl = new HSL(hue, (byte)x, (byte)y);
            var rgb = ColorConverter.HslToRgb(hsl);
            var color = new Rgb24(rgb.R, rgb.G, rgb.B);
            image[x, y] = color;
        }

        sw.Log("hue filled");

        var path = Dir_Debug_HSL
            .EnsureDirectoryExist()
            .Combine($"{nameof(HSL)}-{hue}.png");
        image.SaveAsPng(path);
        sw.Log("image rendered");
    }

    public static void RenderSamplePoster(FilePath path)
    {
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("image is loaded");

        var step = ColorTagService.CalculateStep(source.Size);
        var halfStep = step / 2;
        Log($"using step - {step}");

        var w = source.Width;
        var h = source.Height;
        var image_actual = new Image<Rgb24>(w, h);
        var image_poster = new Image<Rgb24>(w, h);

        string[] keys = ["SL", "S1", "S2", "SD", "PD", "PL"];

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var l = hsl.L;
            var s = hsl.S;

            var hue_index = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues

            var key = (char)('A' + hue_index);
            var posterized = s < 5
                ? l < 20 ? 0.ToRgb24() : l > 80 ? 255.ToRgb24() : 128.ToRgb24()
                : ColorSearchProfile.ColorsFunny[key][$"{key}{keys[s > 50 ? l > 50 ? 1 : 2 : l > 50 ? 5 : 4]}"];

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
                image_poster[x0, y0] = posterized;
            }
        }

        var ticks = DateTime.UtcNow.Ticks >> 32;
        var sand = Desert.GetSand();
        var jpeg1 = Dir_Debug_SampleGrids
            .EnsureDirectoryExist()
            .Combine($"Samples-{ticks}-{sand}.jpg");
        var jpeg2 = Dir_Debug_SampleGrids
            .EnsureDirectoryExist()
            .Combine($"Poster-{ticks}-{sand}.jpg");
        image_actual.SaveAsJpeg(jpeg1, JpegEncoder_Q80);
        image_poster.SaveAsJpeg(jpeg2, JpegEncoder_Q80);
    }

    public static void RenderHSL_Profile(string[] args) => args.ForEachTry(arg => RenderHSL_Profile(arg));
    public static void RenderHSL_Profile(FilePath path)
    {
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("image is loaded");

        using var report = new Image<Rgb24>(SIDE * 3, SIDE * 2, new Rgb24(50, 50, 50));

        for (var row = 0; row < 2; row++)
        for (var col = 0; col < 3; col++)
        {
            report.PutLines(col * SIDE, row * SIDE);
        }

        sw.Log("report is ready");

        var step = ColorTagService.CalculateStep(source.Size);
        Log($"using step - {step}");

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            /*var colors = new[]
            {
                source[x, y],
                source[x + 1, y + 1],
                source[x + 2, y + 0],
                source[x + 0, y + 2],
                source[x + 2, y + 2],
            };
            var sample = ColorHelpers.GetAverageColor(colors);*/

            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var l = hsl.L;
            var s = hsl.S;

            var hue = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues
            var offsetX = hue / 4 * SIDE;
            var offsetY = hue % 2 == 0 ? 0 : SIDE;

            report[offsetX + s, offsetY + l] = sample;
        }

        sw.Log("samples collected");

        var name =  $"HSL-Profile-{Desert.GetSand(8)}.png";
        var save = Dir_Debug_HSL_Profiles
            .EnsureDirectoryExist()
            .Combine(name);
        report.SaveAsPng(save);
        sw.Log($"report was saved as \"{name}\"");
    }

    private static void PutLines(this Image<Rgb24> image, int offsetX = 0, int offsetY = 0)
    {
        Rgb24 c1A = 98.ToRgb24(), c2A = 90.ToRgb24(), c3A = 80.ToRgb24(), c4A = 70.ToRgb24(); 
        Rgb24 c1B = 32.ToRgb24(), c2B = 40.ToRgb24(), c3B = 50.ToRgb24(), c4B = 60.ToRgb24(); 

        image.Mutate(x => x.Fill(c2A, new RectangleF(offsetX +  0, offsetY +  0, SIDE, 50))); // Y-DARK
        image.Mutate(x => x.Fill(c2B, new RectangleF(offsetX +  0, offsetY + 50, SIDE, 51))); // Y-LIGHT
        image.Mutate(x => x.Fill(c1A, new RectangleF(offsetX +  0, offsetY +  0, SIDE,  4))); // BLACK
        image.Mutate(x => x.Fill(c1B, new RectangleF(offsetX +  0, offsetY + 97, SIDE,  4))); // WHITE

        var b1 = ColorTagService.GetGrayscaleLimits();
        var b2 = ColorTagService.GetSubPaleLimits();
        var b3 = ColorTagService.GetPaleLimits();
        var b4 = ColorTagService.GetPeakLimits();
        for (var y = 4; y <= 96; y++)
        {
            var c1 = y < 50 ? c4A : c4B; // pale-weak
            var c2 = y < 50 ? c3A : c3B; // pale-strong
            var c3 = y < 50 ? c4B : c4A; // vivid

            var w1 = b1[y];
            var w2 = b2[y];
            var w3 = b3[y];
            var w4 = b4[y];
            var w5 = Math.Max(w1, w4);
            var w6 = Math.Max(w2, w4);

            DrawLine(y, c1, w1,   w2 - w1);
            DrawLine(y, c2, w2,   w3 - w2);
            DrawLine(y, c3, w3, SIDE - w3);
            DrawLine(y, c2, w5,   w3 - w5);
            DrawLine(y, c1, w6,   w3 - w6);
        }

        return;

        void DrawLine(int y, SixLabors.ImageSharp.Color color, int offset, int width)
        {
            image.Mutate(x => x.Fill(color, new RectangleF(offsetX + offset, offsetY + y, width, 1)));
        }
    }
}
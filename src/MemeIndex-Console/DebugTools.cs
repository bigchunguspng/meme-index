using ColorHelper;
using MemeIndex_Core.Services.ImageAnalysis.Color;
using MemeIndex_Core.Utils;
using MemeIndex_Core.Utils.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace MemeIndex_Console;

public static class DebugTools
{
    private const string DIR = "debug";
    private const int SIDE = 101;

    private static string InDebugDirectory(this string path)
    {
        Directory.CreateDirectory(DIR);
        return Path.Combine(DIR, path);
    }

    public static void LoopHSL(int step)
    {
        for (var hue = 0; hue < 360; hue += step)
        {
            HSL(hue);
        }
    }

    public static void HSL(int hue)
    {
        var sw = Helpers.GetStartedStopwatch();

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

        image.SaveAsPng($"{nameof(HSL)}-{hue}.png".InDebugDirectory());
        sw.Log("image rendered");
    }

    public static void RenderHSL_Profile(string[] args) => args.ForEachTry(RenderHSL_Profile);
    public static void RenderHSL_Profile(string path)
    {
        var sw = Helpers.GetStartedStopwatch();

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
        Logger.Log($"using step - {step}");

        foreach (var (x, y) in new SizeIterator(source.Size, step))
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

        var suffix = Path.GetFileName(Path.GetDirectoryName(path));
        var export = $"HSL-Profile-{path.FancyHashCode()}-{suffix}.png";
        report.SaveAsPng(export.InDebugDirectory());
        sw.Log($"report was saved as \"{export}\"");
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

        void DrawLine(int y, Color color, int offset, int width)
        {
            image.Mutate(x => x.Fill(color, new RectangleF(offsetX + offset, offsetY + y, width, 1)));
        }
    }
}
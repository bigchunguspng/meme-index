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

        using var report = new Image<Rgb24>(303, 202, new Rgb24(50, 50, 50));

        for (var row = 0; row < 2; row++)
        for (var col = 0; col < 3; col++)
        {
            report.PutLines(col * 101, row * 101);
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
            var offsetX = hue / 4 * 101;
            var offsetY = hue % 2 == 0 ? 0 : 101;

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
        image.Mutate(x => x.Fill(new Rgb24(98, 98, 98), new RectangleF(offsetX +  0, offsetY +  0, 101, 4))); // BLACK
        image.Mutate(x => x.Fill(new Rgb24(32, 32, 32), new RectangleF(offsetX +  0, offsetY + 97, 101, 4))); // WHITE
        image.Mutate(x => x.Fill(new Rgb24(90, 90, 90), new RectangleF(offsetX +  0, offsetY +  0, 05, 50))); // Y-DARK
        image.Mutate(x => x.Fill(new Rgb24(40, 40, 40), new RectangleF(offsetX +  0, offsetY + 50, 05, 51))); // Y-LIGHT

        image.Mutate(x => x.Fill(new Rgb24(70, 70, 70), new RectangleF(offsetX + 40, offsetY + 20, 61, 50))); // S-DARK
        image.Mutate(x => x.Fill(new Rgb24(60, 60, 60), new RectangleF(offsetX + 40, offsetY + 50, 61, 31))); // S-LIGHT
        image.Mutate(x => x.Fill(new Rgb24(45, 45, 45), new RectangleF(offsetX + 05, offsetY +  4, 96, 16))); // DARK
        image.Mutate(x => x.Fill(new Rgb24(85, 85, 85), new RectangleF(offsetX + 05, offsetY + 81, 96, 16))); // LIGHT
        image.Mutate(x => x.Fill(new Rgb24(80, 80, 80), new RectangleF(offsetX + 05, offsetY + 12, 35, 38))); // P-DARK
        image.Mutate(x => x.Fill(new Rgb24(50, 50, 50), new RectangleF(offsetX + 05, offsetY + 51, 35, 38))); // P-LIGHT

        var ops = new DrawingOptions { GraphicsOptions = new GraphicsOptions { BlendPercentage = 0.85F } };
        var limits = ColorTagService.GetGrayscaleLimits();
        for (var y = 0; y < limits.Length; y++)
        {
            var value = (byte)(y < 4 ? 98 : y > 96 ? 32 : y < 50 ? 90 : 40);
            var color = new Rgb24(value, value, value);
            image.Mutate(x => x.Fill(ops, color, new RectangleF(offsetX + 0, offsetY + y, limits[y], 1)));
        }
    }
}
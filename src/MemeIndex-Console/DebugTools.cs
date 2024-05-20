using ColorHelper;
using MemeIndex_Core.Services.ImageToText.ColorTag;
using MemeIndex_Core.Utils;
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

        using var report = new Image<Rgb24>(300, 200, new Rgb24(50, 50, 50));
        report.PutLines();
        report.PutLines(100);
        report.PutLines(200);
        report.PutLines(000, 100);
        report.PutLines(100, 100);
        report.PutLines(200, 100);
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
            var l = hsl.L < 100 ? hsl.L : 99;
            var s = hsl.S < 100 ? hsl.S : 99;

            var hue = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues
            var offsetX = hue / 4 * 100;
            var offsetY = hue % 2 == 0 ? 0 : 100;

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
        image.Mutate(x => x.Fill(new Rgb24(90, 90, 90), new RectangleF(offsetX +  0, offsetY +  0, 05, 50))); // Y-DARK
        image.Mutate(x => x.Fill(new Rgb24(40, 40, 40), new RectangleF(offsetX +  0, offsetY + 50, 05, 50))); // Y-LIGHT
        image.Mutate(x => x.Fill(new Rgb24(70, 70, 70), new RectangleF(offsetX + 40, offsetY +  0, 60, 50))); // S-DARK
        image.Mutate(x => x.Fill(new Rgb24(60, 60, 60), new RectangleF(offsetX + 40, offsetY + 50, 60, 50))); // S-LIGHT
        image.Mutate(x => x.Fill(new Rgb24(45, 45, 45), new RectangleF(offsetX + 05, offsetY +  4, 95, 16))); // DARK
        image.Mutate(x => x.Fill(new Rgb24(85, 85, 85), new RectangleF(offsetX + 05, offsetY + 80, 95, 16))); // LIGHT
        image.Mutate(x => x.Fill(new Rgb24(80, 80, 80), new RectangleF(offsetX + 05, offsetY + 12, 35, 38))); // D-DARK
        image.Mutate(x => x.Fill(new Rgb24(50, 50, 50), new RectangleF(offsetX + 05, offsetY + 50, 35, 38))); // D-LIGHT

        image.Mutate(x => x.Fill(new Rgb24(98, 98, 98), new RectangleF(offsetX + 0, offsetY +  0, 100, 4))); // BLACK
        image.Mutate(x => x.Fill(new Rgb24(32, 32, 32), new RectangleF(offsetX + 0, offsetY + 96, 100, 4))); // WHITE
    }
}
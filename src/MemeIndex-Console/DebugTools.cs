using ColorHelper;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    public static void RenderHSL_Profile(string path)
    {
        var sw = Helpers.GetStartedStopwatch();

        using var source = Image.Load<Rgb24>(path);
        using var report = new Image<Rgb24>(300, 200, new Rgb24(50, 50, 50));

        report.PutLines();
        report.PutLines(100);
        report.PutLines(200);
        report.PutLines(000, 100);
        report.PutLines(100, 100);
        report.PutLines(200, 100);

        for (var x = 0; x < source.Width; x += 16)
        for (var y = 0; y < source.Height; y += 16)
        {
            var sample = source[x, y];
            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var l = hsl.L < 100 ? hsl.L : 99;
            var s = hsl.S < 100 ? hsl.S : 99;

            var hue = hsl.H % 360 / 30; // 0..11 => 12 hues
            var offsetX = hue / 4 * 100;
            var offsetY = hue % 2 == 0 ? 0 : 100;

            report[offsetX + s, offsetY + l] = sample;
        }

        sw.Log("colors picked");

        report.SaveAsPng($"HSL-Profile-{Math.Abs(path.GetHashCode())}.png".InDebugDirectory());
        sw.Log("image rendered");
    }

    private static void PutLines(this Image<Rgb24> image, int offsetX = 0, int offsetY = 0)
    {
        image.Mutate(x => x.Fill(new Rgb24(90, 90, 90), new RectangleF(offsetX +  0, offsetY +  0, 10, 50)));
        image.Mutate(x => x.Fill(new Rgb24(40, 40, 40), new RectangleF(offsetX +  0, offsetY + 50, 10, 50)));
        image.Mutate(x => x.Fill(new Rgb24(80, 80, 80), new RectangleF(offsetX + 10, offsetY +  0, 30, 50)));
        image.Mutate(x => x.Fill(new Rgb24(50, 50, 50), new RectangleF(offsetX + 10, offsetY + 50, 30, 50)));
        image.Mutate(x => x.Fill(new Rgb24(70, 70, 70), new RectangleF(offsetX + 40, offsetY +  0, 60, 50)));
        image.Mutate(x => x.Fill(new Rgb24(60, 60, 60), new RectangleF(offsetX + 40, offsetY + 50, 60, 50)));
    }
}
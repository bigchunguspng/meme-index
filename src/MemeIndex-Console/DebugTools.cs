using ColorHelper;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex_Console;

public static class DebugTools
{
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

        image.SaveAsPng($"{nameof(HSL)}-{hue}.png");
        sw.Log("image rendered");
    }

    public static void RenderHSL_Profile(string path)
    {
        var sw = Helpers.GetStartedStopwatch();

        using var source = Image.Load<Rgb24>(path);
        using var paper = new Image<Rgb24>(300, 100, new Rgb24(50, 50, 50));
        
        paper.PutLines();
        paper.PutLines(100);
        paper.PutLines(200);

        for (var x = 0; x < source.Width; x += 16)
        for (var y = 0; y < source.Height; y += 16)
        {
            var picked = source[x, y];
            var rgb = new RGB(picked.R, picked.G, picked.B);
            var hsl = ColorConverter.RgbToHsl(rgb);
            var color = new Rgb24(rgb.R, rgb.G, rgb.B);
            var l = hsl.L < 100 ? hsl.L : 99;
            var s = hsl.S < 100 ? hsl.S : 99;
            var sss = 100 * (hsl.H % 360 / 120) + s;
            paper[sss, l] = color;
        }
        sw.Log("colors picked");

        paper.SaveAsPng($"{nameof(RenderHSL_Profile)}-{Math.Abs(path.GetHashCode())}.png");
        sw.Log("image rendered");
    }

    private static void PutLines(this Image<Rgb24> image, int offset = 0)
    {
        image.Mutate(x => x.Fill(new Rgb24(90, 90, 90), new RectangleF(offset +  0,  0, 10, 50)));
        image.Mutate(x => x.Fill(new Rgb24(40, 40, 40), new RectangleF(offset +  0, 50, 10, 50)));
        image.Mutate(x => x.Fill(new Rgb24(80, 80, 80), new RectangleF(offset + 10,  0, 30, 50)));
        image.Mutate(x => x.Fill(new Rgb24(50, 50, 50), new RectangleF(offset + 10, 50, 30, 50)));
        image.Mutate(x => x.Fill(new Rgb24(70, 70, 70), new RectangleF(offset + 40,  0, 60, 50)));
        image.Mutate(x => x.Fill(new Rgb24(60, 60, 60), new RectangleF(offset + 40, 50, 60, 50)));
    }
}
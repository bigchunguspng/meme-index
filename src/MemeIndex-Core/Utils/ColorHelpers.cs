using MemeIndex_Core.Services.ImageToText.ColorTag;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex_Core.Utils;

public static class ColorHelpers
{
    /// <param name="hue">0 - 360</param>
    /// <param name="saturation">0 - 1</param>
    /// <param name="lightness">0 - 1</param>
    public static Rgba32 ColorFromHSL(double hue, double saturation, double lightness)
    {
        if (saturation == 0)
        {
            var value = lightness.ClampToByte();
            return new Rgba32(value, value, value);
        }

        var h = hue / 360D;

        var max = lightness < 0.5D ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
        var min = lightness * 2D - max;

        return new Rgba32
        (
            (255 * RGBChannelFromHue(min, max, h + 1 / 3D)).ClampToByte(),
            (255 * RGBChannelFromHue(min, max, h /*    */)).ClampToByte(),
            (255 * RGBChannelFromHue(min, max, h - 1 / 3D)).ClampToByte()
        );
    }

    private static double RGBChannelFromHue(double min, double max, double hue)
    {
        hue = (hue + 1D) % 1D;
        if (hue < 0) hue += 1;
        if (hue * 6 < 1) return min + (max - min) * 6 * hue;
        if (hue * 2 < 1) return max;
        if (hue * 3 < 2) return min + (max - min) * 6 * (2D / 3D - hue);
        return min;
    }

    public static int GetHue(this Rgb24 color)
    {
        var r = color.R / 255F;
        var g = color.G / 255F;
        var b = color.B / 255F;

        var max = Math.Max(Math.Max(r, g), b);
        var min = Math.Min(Math.Min(r, g), b);
        var delta = max - min;

        var h = 60F;

        /**/ if (r == max) h *=      (g - b) / delta;
        else if (g == max) h *= 2F + (b - r) / delta;
        else if (b == max) h *= 4F + (r - g) / delta;

        return (int)(h < 0 ? h + 360 : h);
    }

    public static bool IsGrayscale(this Rgba32 c)
    {
        if (c.A < 64) return true;

        var max = Math.Max(Math.Max(c.R, c.G), c.B);
        var min = Math.Min(Math.Min(c.R, c.G), c.B);
        var delta = max - min;
        if (delta < 10) return true;
        
        var lightness = (max + min) / 2;
        var saturation = 255 * delta / (lightness < 127 ? max + min : 510 - max - min);

        return saturation < 28;
    }

    public static int GetDifference(Rgba32 a, Rgba32 b)
    {
        if (a.A < 64 && b.A < 64) return 0;

        return Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
    }

    public static Rgba32 GetAverageColor(Rgba32[] colors)
    {
        if (colors.Max(x => x.A) < 64)
        {
            return ColorSearchProfile.GetTransparent();
        }

        return new Rgba32
        (
            colors.Average(x => x.R).ClampToByte(),
            colors.Average(x => x.G).ClampToByte(),
            colors.Average(x => x.B).ClampToByte()
        );
    }

    public static Rgba32 GetDarkerColor(this Rgba32 color) => new
    (
        ((int)(color.R * 0.75)).ClampToByte(),
        ((int)(color.G * 0.75)).ClampToByte(),
        ((int)(color.B * 0.75)).ClampToByte()
    );

    public static byte ClampToByte(this double value) => (byte)Math.Clamp(value, 0, 255);
    public static byte ClampToByte(this int    value) => (byte)Math.Clamp(value, 0, 255);

    public static string ToCss(this Rgba32 color) => $"rgb({color.R},{color.G},{color.B})";
}
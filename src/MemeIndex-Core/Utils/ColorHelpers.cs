using IronSoftware.Drawing;

namespace MemeIndex_Core.Utils;

public static class ColorHelpers
{
    public static Color ColorFromHSL(double hue, double saturation, double lightness)
    {
        if (saturation == 0)
        {
            var value = (int)lightness;
            return new Color(value, value, value);
        }

        var h = hue / 360D;

        var max = lightness < 0.5D ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
        var min = lightness * 2D - max;

        return new Color
        (
            (int)(255 * RGBChannelFromHue(min, max, h + 1 / 3D)),
            (int)(255 * RGBChannelFromHue(min, max, h)),
            (int)(255 * RGBChannelFromHue(min, max, h - 1 / 3D))
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

    public static int GetHue(this Color color)
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

    public static bool IsGrayscale(this Color c)
    {
        var max = Math.Max(Math.Max(c.R, c.G), c.B);
        var min = Math.Min(Math.Min(c.R, c.G), c.B);
        var delta = max - min;
        if (delta < 10) return true;
        
        var lightness = (max + min) / 2;
        var saturation = 255 * delta / (lightness < 127 ? max + min : 510 - max - min);

        return saturation < 28;
    }

    public static int GetDifference(Color a, Color b)
    {
        var result = Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

        return result;
    }

    public static Color GetAverageColor(Color[] colors)
    {
        if (colors.Max(x => x.A) < 64)
        {
            return new Color(255, 255, 255);
        }

        return new Color
        (
            colors.Sum(x => x.R) / colors.Length,
            colors.Sum(x => x.G) / colors.Length,
            colors.Sum(x => x.B) / colors.Length
        );
    }

    public static Color GetPixelColor(this AnyBitmap image, int x, int y, byte[] pixels, byte[]? alphas)
    {
        if (x < 0 || x >= image.Width)
            throw new ArgumentOutOfRangeException(nameof(x), "x is < 0 or >= Width.");

        if (y < 0 || y >= image.Height)
            throw new ArgumentOutOfRangeException(nameof(y), "y is < 0 or >= Height.");

        var pos1 = y * image.Width + x;
        var pos3 = pos1 * 3;

        var r = pixels[pos3];
        var g = pixels[pos3 + 1];
        var b = pixels[pos3 + 2];

        return alphas is null ? new Color(r, g, b) : new Color(alphas[pos1], r, g, b);
    }

    public static byte[]? GetAlphaBuffer(this AnyBitmap image)
    {
        return image.BitsPerPixel == 32 ? new AnyBitmap(image, image.Width, image.Height).ExtractAlphaData() : null;

        // AnyBitmap needs to be copied because ExtractAlphaData() disposes it.
    }

    public static Color GetDarkerColor(this Color color) => new
    (
        Math.Clamp((int)(color.R * 0.75), 0, 255),
        Math.Clamp((int)(color.G * 0.75), 0, 255),
        Math.Clamp((int)(color.B * 0.75), 0, 255)
    );
}
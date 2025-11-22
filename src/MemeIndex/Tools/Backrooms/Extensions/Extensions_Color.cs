using ColorHelper;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Color
{
    public static Rgb24 ToRgb24(this byte value) => new(value, value, value);
    public static Rgb24 ToRgb24(this int  value)
    {
        var b = value.ClampToByte();
        return new Rgb24(b, b, b);
    }

    public static RGB    ToRGB   (this Rgb24  color) => new(color.R, color.G, color.B);
    public static RGB    ToRGB   (this Rgba32 color) => new(color.R, color.G, color.B);
    public static Rgb24  ToRgb24 (this RGB    color) => new(color.R, color.G, color.B);
    public static Rgba32 ToRgba32(this RGB    color) => new(color.R, color.G, color.B);

    public static Rgb24 GetDarkerColor(this Rgb24 color) => new
    (
        ((int)(color.R * 0.75)).ClampToByte(),
        ((int)(color.G * 0.75)).ClampToByte(),
        ((int)(color.B * 0.75)).ClampToByte()
    );

    public static byte ClampToByte(this int value) => (byte)Math.Clamp(value, 0, 255);

    public static string ToCss(this Rgb24 color) => $"rgb({color.R},{color.G},{color.B})";

    public static HSL ToHsl(this Rgb24 rgb)
    {
        var r = rgb.R / 255.0;
        var g = rgb.G / 255.0;
        var b = rgb.B / 255.0;
        var min = r < g ? r : g < b ? g : b;
        var max = r > g ? r : g > b ? g : b;
        var range = max - min;
        var avg =  (min + max) / 2.0;

        double hue;
        double sat;
        if (range == 0.0)
        {
            hue = 0.0;
            sat = 0.0;
        }
        else
        {
            sat = avg <= 0.5
                ? range /       (min + max)
                : range / (2.0 - max - min);
            hue =    max == r  ?             (g - b) / 6.0 / range
                :    max == g  ? 1.0 / 3.0 + (b - r) / 6.0 / range
                : /* max == b */ 2.0 / 3.0 + (r - g) / 6.0 / range;
            hue = hue switch
            {
                < 0.0 => hue % 1.0 + 1.0,
                > 1.0 => hue % 1.0,
                _     => hue,
            };
        }

        var h =  (int) Math.Round(hue * 360.0);
        var s = (byte) Math.Round(sat * 100.0);
        var l = (byte) Math.Round(avg * 100.0);
        return new HSL(h, s, l);
    }
}
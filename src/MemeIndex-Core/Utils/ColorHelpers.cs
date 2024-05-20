using ColorHelper;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex_Core.Utils;

public static class ColorHelpers
{

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
}
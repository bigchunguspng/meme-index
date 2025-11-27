using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Image
{
    /// To draw a pixel-art, encode it as a multiline string,
    /// where painted pixels are represented by '#' char.
    public static void DrawPixelArt<T>
        (this Image<T> image, string bitmap, T color, Point point)
        where T : unmanaged, IPixel<T>
    {
        var y = 0;
        using var reader = new StringReader(bitmap);
        while (reader.ReadLine() is { } line)
        {
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '#') image[point.X + i, point.Y + y] = color;
            }

            y++;
        }
    }
}
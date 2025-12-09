using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Image
{
    private static readonly string[] ASCII_PRINTABLE =
        // Generator: https://patorjk.com/software/taag, Font: Letter.
        // 95 chars:
        //  !"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~
        [
            "     |#    |# #  | # # | ### |##  #| #   |#    | #   |#    |     |     |     |     |     |    #| ### |  #  | ### |#####|   # |#####| ### |#####| ### | ### |     |     |   # |     | #   | ### | ### |  #  |#### | ### |#### |#####|#####| ### |#   #|###  |  ###|#   #|#    |#   #|#   #| ### |#### | ### |#### | ####|#####|#   #|#   #|#   #|#   #|#   #|#####|##   |#    |##   | #   |     |#    |  #  |#### | ### |#### |#####|#####| ### |#   #|###  |  ###|#   #|#    |#   #|#   #| ### |#### | ### |#### | ####|#####|#   #|#   #|#   #|#   #|#   #|#####| ##  | #   |##   |     |",
            "     |#    |# #  |#####|# #  |## # |# #  |#    |#    | #   | # # |  #  |     |     |     |   # |#   #| ##  |#   #|   # |  ## |#    |#    |    #|#   #|#   #|     |     |  #  | ### |  #  |#   #|#   #| # # |#   #|#   #|#   #|#    |#    |#    |#   #| #   |   # |#  # |#    |## ##|##  #|#   #|#   #|#   #|#   #|#    |  #  |#   #|#   #|#   #| # # | # # |   # |#    | #   | #   |# #  |     | #   | # # |#   #|#   #|#   #|#    |#    |#    |#   #| #   |   # |#  # |#    |## ##|##  #|#   #|#   #|#   #|#   #|#    |  #  |#   #|#   #|#   #| # # | # # |   # | #   | #   | #   | #   |",
            "     |#    |     | # # | ### |  #  | ## #|     |#    | #   |  #  | ### |     | ### |     |  #  |# # #|  #  |  ## |  ## | # # |#### |#### |   # | ### | ####|#    |#    | #   |     |   # |   # |# ## |#####|#### |#    |#   #|#### |#### |# ## |#####| #   |   # |###  |#    |# # #|# # #|#   #|#### |# # #|#### | ### |  #  |#   #|#   #|# # #|  #  |  #  |  #  |#    |  #  | #   |     |     |     |#####|#### |#    |#   #|#### |#### |# ## |#####| #   |   # |###  |#    |# # #|# # #|#   #|#### |# # #|#### | ### |  #  |#   #|#   #|# # #|  #  |  #  |  #  |##   | #   | ##  |# # #|",
            "     |     |     |#####|  # #| # ##|#  # |     |#    | #   | # # |  #  |     |     |     | #   |#   #|  #  | #   |#   #|#####|    #|#   #|  #  |#   #|    #|     |     |  #  | ### |  #  |     |# ## |#   #|#   #|#   #|#   #|#    |#    |#   #|#   #| #   |#  # |#  # |#    |#   #|#  ##|#   #|#    |#  # |#  # |    #|  #  |#   #| # # |# # #| # # |  #  | #   |#    |   # | #   |     |     |     |#   #|#   #|#   #|#   #|#    |#    |#   #|#   #| #   |#  # |#  # |#    |#   #|#  ##|#   #|#    |#  # |#  # |    #|  #  |#   #| # # |# # #| # # |  #  | #   | #   | #   | #   |   # |",
            "     |#    |     | # # | ### |#  ##| ## #|     | #   |#    |     |     |#    |     |#    |#    | ### | ### |#####| ### |   # |#### | ### | #   | ### | ### |#    |#    |   # |     | #   |  #  |#    |#   #|#### | ### |#### |#####|#    | ### |#   #|###  | ##  |#   #|#####|#   #|#   #| ### |#    | ## #|#   #|#### |  #  | ### |  #  | # # |#   #|  #  |#####|##   |    #|##   |     |#####|     |#   #|#### | ### |#### |#####|#    | ### |#   #|###  | ##  |#   #|#####|#   #|#   #| ### |#    | ## #|#   #|#### |  #  | ### |  #  | # # |#   #|  #  |#####| ##  | #   |##   |     |",
            "     |     |     |     |     |     |     |     |     |     |     |     |#    |     |     |     |     |     |     |     |     |     |     |     |     |     |     |#    |     |     |     |     | ####|     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |     |",
        ];

    private static int GetASCII_CharWidth(char c) => c switch
    {
        'I' => 3, 'i' => 3,
        '!' => 1, '"' => 3, '\'' => 1,
        '.' => 1, ',' => 1,
        ':' => 1, ';' => 1,
        '^' => 3, '`' => 2,
        '(' => 2, ')' => 2,
        '[' => 2, ']' => 2,
        '{' => 3, '}' => 3,
        '|' => 3, _   => 5,
    };

    /// <inheritdoc cref="DrawASCII{T}"/>
    public static Point DrawASCII_Shady<T>
        (this Image<T> image, ReadOnlySpan<char> text, T colorText, T colorShadow, Point point)
        where T : unmanaged, IPixel<T>
    {
        _    = image.DrawASCII(text, colorShadow, point + new Size(1, 1));
        return image.DrawASCII(text, colorText,   point);
    }

    /// Draw text with a 5x6 bitmap font (variable char width). <br/>
    /// Pass only printable ASCII text (0x20 ' ' - 0x7E '~') and 0x0A '\n'! <br/>
    /// Method returns a point which can be used to continue draw text.
    public static Point DrawASCII<T>
        (this Image<T> image, ReadOnlySpan<char> text, T color, Point point)
        where T : unmanaged, IPixel<T>
    {
        const int H = 6, OFFSET = 6, LINE_H = 7;
        var (px, py) = point;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                px = point.X;
                py += LINE_H;
                continue;
            }

            var ci = c - ' ';
            var cx = ci * OFFSET;
            var w = GetASCII_CharWidth(c);
            for (var y = 0; y < H; y++)
            for (var x = 0; x < w; x++)
            {
                if (ASCII_PRINTABLE[y][cx + x] == '#')
                    image[px + x, py + y] = color;
            }

            px += w + 1;
        }

        return new Point(px, py);
    }

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

    /// Switches colors a & b inside given area of the image.
    public static void SwitchColors
        (this Image<Rgb24> image, Rgb24 a, Rgb24 b, Rectangle area)
    {
        int
            y0 = area.Y, yN = area.Bottom,
            x0 = area.X, xN = area.Right;

        for (var y = y0; y < yN; y++)
        for (var x = x0; x < xN; x++)
        {
            var pixel = image[x,y];
            if      (pixel == a) image[x, y] = b;
            else if (pixel == b) image[x, y] = a;
        }
    }
}
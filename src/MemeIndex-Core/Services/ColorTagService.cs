using IronSoftware.Drawing;

namespace MemeIndex_Core.Services;

public class ColorTagService
{
    public Dictionary<string, Color> Colors { get; } = new();

    public List<char> Hues { get; } = new()
    {
        'R', 'o', 'Y', 's', // Red      orange  Yellow  spring
        'G', 'g', 'C', 'h', // Green    grass   Cyan    heaven
        'B', 'p', 'M', 'f', // Blue     purple  Magenta flamingo
    };

    public ColorTagService()
    {
        Init();
    }

    public Task<string?> GetImageColorInfo(string path)
    {
        var file = new FileInfo(path);
        if (file.Exists == false)
        {
            return Task.FromResult<string?>(null);
        }

        using var image = AnyBitmap.FromFile(path);

        var w = image.Width;
        var h = image.Height;

        var stepX = w < 32 ? w / 3 : w < 128 ? 12 : w > 320 ? w / 12 : 16;
        var stepY = h < 32 ? h / 3 : h < 128 ? 12 : h > 320 ? h / 12 : 16;

        var dots = new List<KeyValuePair<string, Color>>();

        for (var x = w % stepX / 2; x < w; x += stepX)
        for (var y = h % stepY / 2; y < h; y += stepY)
        {
            var color = image.GetPixel(x, y);
            dots.Add(FindClosestColor(color));
        }

        var offset = h / stepY + 1;
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            image.SetPixel(x, y, dots[Math.Clamp(x / stepX * offset + y / stepY, 0, dots.Count - 1)].Value);
        }

        image.SaveAs("baka.jpg", AnyBitmap.ImageFormat.Jpeg, 50);

        var result = string.Join(' ', dots.Select(x => x.Key).Distinct().OrderBy(x => x));
        return Task.FromResult<string?>(result);
        
        // todo make color picker less vulnerable to details
    }

    private KeyValuePair<string, Color> FindClosestColor(Color color)
    {
        return Colors.MinBy(x => GetDifference(x.Value, color));
    }

    private static int GetDifference(Color a, Color b)
    {
        return Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
    }

    private void PrintColors()
    {
        const int w = 16 * 12;
        const int h = 16 * 6;

        using var image = new AnyBitmap(w, h);

        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            image.SetPixel(x, y, Colors.ElementAt(5 + x / 16 + y / 16 * 12).Value);
        }

        image.SaveAs("colors.png");
    }

    private void Init()
    {
        // calculate colors

        for (var i = 0; i < 5; i++) // WHITE & BLACK
        {
            var value = Math.Max(0, i * 64 - 1);
            Colors.Add($"wb{i}", new Color(value, value, value));
        }

        for (var hue = 0; hue < 360; hue += 30)
        for (var sat = 0; sat < 2; sat++)
        for (var lig = 1; lig < 4; lig++)
        {
            var saturation = 1D - sat * 0.5D;
            var lightness  = 1D - lig * 0.25D;
            var code = $"{Hues[hue / 30]}{sat}{lig}";
            Colors.Add(code, ColorFromHSL(hue, saturation, lightness));
        }
    }

    private static Color ColorFromHSL(double hue, double saturation, double lightness)
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
}
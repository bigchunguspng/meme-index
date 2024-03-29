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

    public Task<string?>? GetImageColorInfo(string path)
    {
        var file = new FileInfo(path);
        if (file.Exists == false)
        {
            return null;
        }

        using var image = AnyBitmap.FromFile(path);

        var w = image.Width;
        var h = image.Height;

        var stepX = w < 32 ? w / 3 : w < 128 ? 12 : 16;
        var stepY = h < 32 ? h / 3 : h < 128 ? 12 : 16;

        for (var x = w % stepX / 2; x < w; x += stepX)
        for (var y = h % stepY / 2; y < h; y += stepY)
        {
            image.SetPixel(x, y, new Color(255, 0, 0));
        }

        image.SaveAs("baka.jpg", AnyBitmap.ImageFormat.Jpeg, 50);
    
        //var color = image.GetPixel(16, 16);

        //

        return null;
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
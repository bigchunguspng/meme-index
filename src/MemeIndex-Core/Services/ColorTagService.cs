using IronSoftware.Drawing;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Services;

public class ColorTagService
{
    public Dictionary<char,   Dictionary<string, Color>> ColorsFunny     { get; } = new();
    public Dictionary<string, Color>                     ColorsGrayscale { get; } = new();

    public List<char> Hues { get; } = Enumerable.Range(65, 24).Select(x => (char)x).ToList();

    public ColorTagService()
    {
        Init();
    }

    public string? GetImageColorInfo(string path)
    {
        var file = new FileInfo(path);
        if (file.Exists == false)
        {
            return null;
        }

        using var image = AnyBitmap.FromFile(path);

        var w = image.Width;
        var h = image.Height;

        var margin = Math.Min(w, h) < 80 ? 1 : 2;

        var w2 = w - 2 * margin;
        var h2 = h - 2 * margin;

        var stepX = GetStep(w2);
        var stepY = GetStep(h2);

        int GetStep(int side) => side < 80 ? Math.Max(side / 8, 4) : side >> 4;

        var chunksX = w2 / stepX;
        var chunksY = h2 / stepY;

        var dots = new List<KeyValuePair<string, Color>>();

        var pixels = image.GetRGBBuffer();
        var alphas = image.GetAlphaBuffer();

        for (var x = (w - stepX * (chunksX - 1)) / 2; x < w - margin; x += stepX)
        for (var y = (h - stepY * (chunksY - 1)) / 2; y < h - margin; y += stepY)
        {
            var colors = new Color[4];
            var index = 0;
            for (var i = -margin; i <= margin; i += 2 * margin)
            for (var j = -margin; j <= margin; j += 2 * margin)
            {
                colors[index++] = image.GetPixelColor(x + i, y + j, pixels, alphas);
#if DEBUG
                image.SetPixel(x + i, y + j, Color.Red);
#endif
            }

            var color = FindClosestKnownColor(ColorHelpers.GetAverageColor(colors));
            dots.Add(color);
        }

#if DEBUG
        image.SaveAs("baka-dots.jpg", AnyBitmap.ImageFormat.Jpeg, 50);

        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            var dot = x / stepX * chunksY + y / stepY;
            image.SetPixel(x, y, dots[Math.Clamp(dot, 0, dots.Count - 1)].Value);
        }

        image.SaveAs("baka-colors.jpg", AnyBitmap.ImageFormat.Jpeg, 50);
#endif

        var data = dots
            .Select(x => x.Key)
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .Select(x => x.Key);

        return string.Join(' ', data);
    }

    private KeyValuePair<string, Color> FindClosestKnownColor(Color color)
    {
        var chunk = color.IsGrayscale()
            ? ColorsGrayscale
            : ColorsFunny[Hues[color.GetHue() / 15]];
        return chunk.MinBy(x => ColorHelpers.GetDifference(x.Value, color));
    }

    private void PrintColors()
    {
        var w = 16 * ColorsFunny.Count;
        var h = 16 * ColorsFunny.First().Value.Count;

        using var image = new AnyBitmap(w, h);

        for (var x = 0; x < w; x++)
        {
            var hue = ColorsFunny[(char)(65 + x / 16)];
            for (var y = 0; y < h; y++)
            {
                image.SetPixel(x, y, hue.ElementAt(y / 16).Value);
            }
        }

        image.SaveAs("colors.png");
    }

    private void Init()
    {
        for (var i = 0; i < 5; i++) // WHITE & BLACK
        {
            var value = Math.Max(0, i * 64 - 1);
            ColorsGrayscale.Add($"_{i}", new Color(value, value, value));
        }

        for (var h = 0; h < 360; h += 15)
        {
            var hue = Hues[h / 15];
            ColorsFunny[hue] = new Dictionary<string, Color>();

            for (var l = 0; l <= 6; l++)
            {
                var lightness = Math.Clamp(0.95D - l * 0.15D, 0.1D, 0.9D);
                ColorsFunny[hue].Add($"{hue}{l}", ColorHelpers.ColorFromHSL(h, 0.75D, lightness));
            }
        }
    }
}
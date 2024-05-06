using IronSoftware.Drawing;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Services.ImageToText.ColorTag;

public class ColorSearchProfile
{
    public ColorSearchProfile() => Init();

    public Dictionary<char, Dictionary<string, Color>> ColorsFunny     { get; } = new();
    public                  Dictionary<string, Color>  ColorsGrayscale { get; } = new();

    public List<char> Hues { get; } = Enumerable.Range(65, 24).Select(x => (char)x).ToList();

    private void Init()
    {
        for (var i = 0; i < 5; i++) // WHITE & BLACK
        {
            var value = Math.Max(0, 255 - i * 64);
            ColorsGrayscale.Add($"_{i}", new Color(value, value, value));
        }

        ColorsGrayscale.Add("_-", new Color(0, 0, 128, 255)); // TRANSPARENT

        for (var h = 0; h < 360; h += 15) // SATURATED
        {
            var hue = Hues[h / 15];
            ColorsFunny[hue] = new Dictionary<string, Color>();

            for (var l = 0; l <= 5; l++)
            {
                var lightness = Math.Clamp(0.95D - l * 0.15D, 0.1D, 0.9D);
                ColorsFunny[hue].Add($"{hue}{l}", ColorHelpers.ColorFromHSL(h, 0.75D, lightness));
            }
        }
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
}
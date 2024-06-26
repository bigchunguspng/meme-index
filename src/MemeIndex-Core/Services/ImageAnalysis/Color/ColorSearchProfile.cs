using ColorHelper;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex_Core.Services.ImageAnalysis.Color;

public class ColorSearchProfile
{
    public ColorSearchProfile() => Init();

    private const int HUE_RANGE = 30;
    public  const int HUE_COUNT = 12;

    public Dictionary<char, ColorPalette> ColorsFunny     { get; } = new();
    public                  ColorPalette  ColorsGrayscale { get; } = new();

    public List<char> Hues { get; } = Enumerable.Range(65, 12).Select(x => (char)x).ToList();

    public string CodeTransparent => "X";

    public string[] GetShadesByHue(int i)
    {
        var key = Hues[i];
        return ColorsFunny[key].Keys.ToArray();
    }

    private void Init()
    {
        var grayscale = new byte[]
        {
            255,
            16 + 30 + 60 * 3,
            16 + 30 + 60 * 2,
            16 + 30 + 60,
            16 + 30,
            0,
        };
        for (var i = 0; i < grayscale.Length; i++) // WHITE & BLACK
        {
            var value = grayscale[i];
            ColorsGrayscale.Add($"Y{i}", new Rgb24(value, value, value));
        }

        var saturated = new byte[]
        {
            100 - 12, // LIGHT
            100 - 40,
            000 + 40,
            000 + 16, // DARK
        };

        var saturatedCodes = new[] { 'L', '1', '2', 'D' };

        for (var h = 0; h < 360; h += HUE_RANGE)
        {
            // SATURATED

            var key = Hues[h / HUE_RANGE];
            ColorsFunny[key] = new ColorPalette();

            for (var i = 0; i < saturated.Length; i++)
            {
                var rgb = ColorConverter.HslToRgb(new HSL(h, 75, saturated[i]));
                ColorsFunny[key].Add($"{key}S{saturatedCodes[i]}", rgb.ToRgb24());
            }

            // DESATURATED

            ColorsFunny[key].Add($"{key}PD", ColorConverter.HslToRgb(new HSL(h, 25, 28)).ToRgb24());
            ColorsFunny[key].Add($"{key}PL", ColorConverter.HslToRgb(new HSL(h, 20, 64)).ToRgb24());
        }
    }
}

public class ColorPalette : Dictionary<string, Rgb24>;
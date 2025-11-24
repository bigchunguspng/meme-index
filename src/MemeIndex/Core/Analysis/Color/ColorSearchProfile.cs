using ColorHelper;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color;

public static class ColorSearchProfile
{
    public const int
        HUE_RANGE_deg = 30,
        HUE_COUNT     = 12; // 360 / 30

    public static Dictionary<char, ColorPalette> ColorsFunny     { get; } = new();
    public static                  ColorPalette  ColorsGrayscale { get; } = new();

    public static char[] Hues { get; } = HUE_COUNT.Times(i => (char)('A' + i));

    public static string CodeTransparent => "X";

    public static string[] GetShadesByHue(int i) => ColorsFunny[Hues[i]].Keys.ToArray();

    static ColorSearchProfile()
    {
        char[] saturatedCodes = [ 'L', '1', '2', 'D' ];
        byte[] grayscale =
            [
                255,
                16 + 30 + 60 * 3,
                16 + 30 + 60 * 2,
                16 + 30 + 60,
                16 + 30,
                0,
            ],
            saturated =
            [
                100 - 12, // LIGHT
                100 - 40,
                000 + 40,
                000 + 16, // DARK
            ];

        for (var i = 0; i < grayscale.Length; i++)
        {
            // WHITE & BLACK

            var value = grayscale[i];
            ColorsGrayscale.Add($"Y{i}", new Rgb24(value, value, value));
        }

        for (var h = 0; h < 360; h += HUE_RANGE_deg)
        {
            // SATURATED

            var key = Hues[h / HUE_RANGE_deg];
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
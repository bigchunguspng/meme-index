using ColorHelper;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex_Core.Services.ImageToText.ColorTag;

public class ColorSearchProfile
{
    public ColorSearchProfile() => Init();

    public const int HUE_RANGE = 30, HUES_TOTAL = 360 / HUE_RANGE;

    // todo Rgba32 -> Rgb24

    public Dictionary<char, ColorPalette> ColorsFunny     { get; } = new();
    public                  ColorPalette  ColorsGrayscale { get; } = new();

    public List<char> Hues  { get; } = Enumerable.Range(65, 12).Select(x => (char)x).ToList();

    public string CodeTransparent => "X";

    public string[] GetShadesByHue(int i)
    {
        var key = Hues[i];
        return ColorsFunny[key].Keys.ToArray();
    }

    public static Rgba32 GetTransparent() => new(255, 127, 0, 0);

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
            ColorsGrayscale.Add($"Y{i}", new Rgba32(value, value, value));
        }

        // todo remove (after not needed in gtk app)
        ColorsGrayscale.Add("X", GetTransparent()); // TRANSPARENT

        var saturated = new byte[]
        {
            100 - 12, // LIGHT
            100 - 40,
            000 + 40,
            000 + 16, // DARK
        };

        for (var h = 0; h < 360; h += HUE_RANGE)
        {
            // SATURATED

            var key = Hues[h / HUE_RANGE];
            ColorsFunny[key] = new ColorPalette();

            for (var i = 0; i < saturated.Length; i++)
            {
                var rgb = ColorConverter.HslToRgb(new HSL(h, 75, saturated[i]));
                ColorsFunny[key].Add($"{key}S{i + 1}", rgb.ToRgba32());
            }

            // DESATURATED

            ColorsFunny[key].Add($"{key}PD", ColorConverter.HslToRgb(new HSL(h, 25, 28)).ToRgba32());
            ColorsFunny[key].Add($"{key}PL", ColorConverter.HslToRgb(new HSL(h, 20, 64)).ToRgba32());
        }
    }
}

public class ColorPalette : Dictionary<string, Rgba32>;
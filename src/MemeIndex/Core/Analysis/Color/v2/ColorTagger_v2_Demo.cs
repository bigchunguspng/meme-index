using ColorHelper;
using SixLabors.ImageSharp.PixelFormats;
using Spectre.Console;

namespace MemeIndex.Core.Analysis.Color.v2;

public static class ColorTagger_v2_Demo
{
    public static void PrintPalette()
    {
        var palette = ColorAnalyzer_v2.GetPalette();
        foreach (var set in palette)
        {
            foreach (var point in set.Points)
            {
                var bg_rgb = point.ToRgb24();
                var bg = ColorConverter.RgbToHex(bg_rgb.ToRGB());
                var fg = bg_rgb.ToOklch().L > 0.6 ? "black" : "white";
                AnsiConsole.Markup($"[{fg} on #{bg}] ABCDEF [/]");
            }

            Console.WriteLine();
        }
    }

    public static void Run(string path)
    {
        DebugTools.RenderProfile_Oklch_v2(path);

        var tags = ColorTagger_v2.AnalyzeImage(path).Result.OrderByDescending(x => x.Score).ToArray();
        if (tags.Length == 0)
        {
            Print("NO TAGS, IMAGE EMPTY");
            return;
        }

        Console.WriteLine("\nCOLORS FOUND: " + tags.Length);
        var rows = Math.Ceiling(tags.Length / 4.0);
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                var i = (int)(rows * col + row);
                if (i >= tags.Length) break;

                var (term, score) = tags[i];

                Rgb24 bg_rgb;
                var special = false;
                if      (term[0] == ColorTagger_v2.KEY_GRAY)
                {
                    var l = (term[1] - '0') * 25;
                    bg_rgb = new HSL(0, 0, (byte)l.Cap(100)).ToRgb24();
                }
                else if (term[0] == ColorTagger_v2.KEY_WEAK)
                {
                    var hc = ColorTagger_v2.KEYS_HC.IndexOf(term[1]);
                    var dl = ColorTagger_v2.KEYS_DL.IndexOf(term[2]);
                    bg_rgb = _palette_W[2 * hc + dl];
                }
                else if (term[0] == ColorTagger_v2.KEY_MISC)
                {
                    bg_rgb = 0.ToRgb24();
                    special = true;
                }
                else
                {
                    var hue_ix = ColorTagger_v2.KEYS_HUE.IndexOf(term[0]);
                    var opt_ix = ColorTagger_v2.KEYS_OPT.IndexOf(term[1]);
                    bg_rgb = _palette_H[6 * hue_ix + opt_ix];
                }

                var bg = ColorConverter.RgbToHex(bg_rgb.ToRGB());
                var fg = special ? "gold1" : bg_rgb.ToOklch().L > 0.5 ? "black" : "white";
                AnsiConsole.Markup($"[{fg} on #{bg}]\t {term,3} - {score,5} [/]");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    private static readonly Rgb24[]
        _palette_W = GeneratePalette_Weak(),
        _palette_H = GeneratePalette_Hue();

    private static Rgb24[] GeneratePalette_Weak()
    {
        var palette = new Rgb24[4];
        var i = 0;
        foreach (var H in new [] { 32, 212 })
        foreach (var L in new [] { 0.25, 0.75 })
        {
            palette[i++] = new Oklch(L, 0.035, H).ToRgb24();
        }

        return palette;
    }

    private static Rgb24[] GeneratePalette_Hue()
    {
        var palette = new Rgb24[6 * ColorAnalyzer_v2.N_HUES];
        var refs = ColorAnalyzer_v2.GetPalette().ToArray();
        for (var h = 0; h < ColorAnalyzer_v2.N_HUES;  h++)
        for (var o = 0; o < ColorAnalyzer_v2.N_OPS_H; o++)
        {
            palette[ColorAnalyzer_v2.N_OPS_H * h + o] = refs[h][o].ToRgb24();
        }

        return palette;
    }

    private static Rgb24[] GeneratePalette_Hue_old()
    {
        var palette = new Rgb24[6 * ColorAnalyzer_v2.N_HUES];
        var hues = new [] {0, 30, 60, 75, 110, 165, 190, 240, 265, 315};
        for (var h = 0; h < hues.Length; h++)
        {
            // BPDL01
            palette[6 * h + 0] = ColorConverter.HslToRgb(new HSL(hues[h], 100, 50)).ToRgb24();
            palette[6 * h + 1] = ColorConverter.HslToRgb(new HSL(hues[h],  30, 50)).ToRgb24();
            palette[6 * h + 2] = ColorConverter.HslToRgb(new HSL(hues[h],  80, 15)).ToRgb24();
            palette[6 * h + 3] = ColorConverter.HslToRgb(new HSL(hues[h],  80, 85)).ToRgb24();
            palette[6 * h + 4] = ColorConverter.HslToRgb(new HSL(hues[h],  50, 10)).ToRgb24();
            palette[6 * h + 5] = ColorConverter.HslToRgb(new HSL(hues[h],  50, 90)).ToRgb24();
        }

        return palette;
    }
}
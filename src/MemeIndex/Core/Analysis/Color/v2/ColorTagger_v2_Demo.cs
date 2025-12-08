using ColorHelper;
using MemeIndex.Core.Analysis.Color.v1;
using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        const int
            PAD = 6,
            IMAGE_WH = 392,
            PROFILE_TILE_WH = 101,
            PROFILE_H = 2 * PROFILE_TILE_WH,
            PROFILE_W = 3 * PROFILE_TILE_WH,
            W = IMAGE_WH + PROFILE_W + 3 * PAD,
            H = IMAGE_WH + 100 + 3 * PAD, // todo 100 -> ?
            TAG_WH = 18,
            TAG_TABLE_W = ColorAnalyzer_v2.N_HUES  * TAG_WH,
            TAG_TABLE_H = ColorAnalyzer_v2.N_OPS_H * TAG_WH,
            CHAR_WH = 6,
            CHAR_PAD = (TAG_WH - CHAR_WH) / 2,
            LABEL_PAD = 3,
            LABEL_WH = CHAR_WH + LABEL_PAD,
            TAG_TABLE_FULL_W = LABEL_WH + TAG_TABLE_W,
            TAG_TABLE_FULL_H = LABEL_WH + TAG_TABLE_H;

        const int // columns, rows
            C0 = PAD,
            C1 = C0 + IMAGE_WH + PAD,
            C0R0 = PAD,
            C0R1 = C0R0 + IMAGE_WH + PAD,
            C1R0 = PAD,
            C1R1 = C1R0 + PROFILE_H + PAD,
            C1R2 = C1R1 + TAG_TABLE_FULL_H + PAD;

        // ANALYZE IMAGE
        var tags = ColorTagger_v2.AnalyzeImage(path).Result.OrderByDescending(x => x.Score).ToArray();
        if (tags.Length == 0)
        {
            Print("NO TAGS, IMAGE EMPTY");
            return;
        }

        // REPORT
        using var source = Image.Load<Rgb24>(path);
        using var report = new Image<Rgb24>(W, H, 64.ToRgb24());

        var colorText = 120.ToRgb24();
        var colorTagHl = 68.ToRgb24();

        // TAGS BY CATEGORY
        {
            const int
                x0 = C1,
                y0 = C1R1,
                xl = x0 + LABEL_WH,
                yl = y0 + LABEL_WH;

            // LABELS - CHROMATIC
            for (var hi = 0; hi < ColorAnalyzer_v2.N_HUES; hi++)
            {
                var key = ColorTagger_v2.KEYS_HUE.AsSpan(hi, 1);
                var x = xl + hi * TAG_WH + CHAR_PAD;
                report.DrawASCII(key, colorText, new Point(x, y0));
            }
            for (var oi = 0; oi < ColorAnalyzer_v2.N_OPS_H; oi++)
            {
                var key = ColorTagger_v2.KEYS_OPT.AsSpan(oi, 1);
                var y = yl + oi * TAG_WH + CHAR_PAD;
                report.DrawASCII(key, colorText, new Point(x0, y));
            }

            // LABELS - ACHROMATIC
            {
                var x = x0 + TAG_TABLE_FULL_W + PAD + LABEL_WH + CHAR_PAD;
                report.DrawASCII($"{ColorTagger_v2.KEY_GRAY}", colorText, new Point(x, y0));
            }
            for (var i = 0; i < ColorAnalyzer_v2.N_OPS_G; i++)
            {
                var key = $"{(char)('0' + i)}";
                var x = x0 + TAG_TABLE_FULL_W + PAD;
                var y = yl + i * TAG_WH + CHAR_PAD;
                report.DrawASCII(key, colorText, new Point(x, y));
            }

            var tag_scores = tags.ToDictionary(x => x.Term, x => x.Score);

            // TAGS - CHROMATIC
            for (var oi = 0; oi < ColorAnalyzer_v2.N_OPS_H; oi++)
            for (var hi = 0; hi < ColorAnalyzer_v2.N_HUES;  hi++)
            {
                var xi = xl + TAG_WH * hi;
                var yi = yl + TAG_WH * oi;
                var key = $"{ColorTagger_v2.KEYS_HUE[hi]}{ColorTagger_v2.KEYS_OPT[oi]}";
                var i = ColorAnalyzer_v2.N_OPS_H * hi + oi;
                DrawTagSquare(xi, yi, key, () => _palette_H[i]);
            }

            // TAGS - ACHROMATIC
            for (var i = 0; i < ColorAnalyzer_v2.N_OPS_G; i++)
            {
                var xi = xl + TAG_TABLE_W + PAD + LABEL_WH;
                var yi = yl + i * TAG_WH;
                var key = $"{ColorTagger_v2.KEY_GRAY}{i}";
                var i_ = i;
                DrawTagSquare(xi, yi, key, () =>
                {
                    var L = ColorAnalyzer_v2.GrayReferences_L[i_];
                    var l = (L * 100).RoundInt().Cap(100);
                    return new HSL(0, 0, (byte)l).ToRgb24();
                });
            }

            void DrawTagSquare(int x, int y, string key, Func<Rgb24> getColor)
            {
                if (tag_scores.TryGetValue(key, out var score))
                {
                    var side = (float)(score * 0.016).FastPow(0.5);
                    var gap  = TAG_WH.Gap((int)side).RoundInt();
                    report.Mutate(ctx => ctx.Fill(colorTagHl, new RectangleF(x, y, TAG_WH, TAG_WH)));
                    report.Mutate(ctx => ctx.Fill(getColor(), new RectangleF(x + gap, y + gap, side, side)));
                }
                else
                    report.DrawASCII("!", 48.ToRgb24(), new Point(x + CHAR_PAD + 2, y + CHAR_PAD));
            }
        }

        // OTHER INFO
        {
            report.DrawASCII($"TAGS: {tags.Length,3}", colorText, new Point(C0, C0R1));
        }

        // OKLCH v2 PROFILE
        using var profile = DebugTools.GetReportBackground_Oklch(useMagenta: false);
        var step = ColorTagService.CalculateStep(source.Size);
        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];
            DebugTools.PutSample_On_Profile_Oklch_v2(profile, sample);
        }
        report.Mutate(ctx => ctx.DrawImage(profile, new Point(C1, C1R0), 1F));

        // PUT IMAGE
        {
            var size = source.Size.FitSize(392);
            var x = PAD + IMAGE_WH.Gap(size.Width ).RoundInt();
            var y = PAD + IMAGE_WH.Gap(size.Height).RoundInt();
            source.Mutate(ctx => ctx.Resize(size));
            report.Mutate(ctx => ctx.DrawImage(source, new Point(x, y), 1F));
        }

        // SAVE REPORT
        var name = $"Profile-{DateTime.UtcNow.Ticks:x16}-{Desert.GetSand()}-Analysis.png";
        var save = Dir_Debug_Mixed.EnsureDirectoryExist().Combine(name);
        report.SaveAsPng(save);
    }

    public static void Run_console(string path)
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
                    var L = ColorAnalyzer_v2.GrayReferences_L[term[1] - '0'];
                    var l = (L * 100).RoundInt().Cap(100);
                    bg_rgb = new HSL(0, 0, (byte)l).ToRgb24();
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
        var palette = new Rgb24[ColorAnalyzer_v2.N_HUES * ColorAnalyzer_v2.N_OPS_H];
        var refs = ColorAnalyzer_v2.GetPalette().ToArray();
        for (var h = 0; h < ColorAnalyzer_v2.N_HUES;  h++)
        for (var o = 0; o < ColorAnalyzer_v2.N_OPS_H; o++)
        {
            palette[ColorAnalyzer_v2.N_OPS_H * h + o] = refs[h][o].ToRgb24();
        }

        return palette;
    }
}
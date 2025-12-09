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
            TAG_WH = 18,
            TAG_TABLE_W = ColorAnalyzer_v2.N_HUES  * TAG_WH,
            TAG_TABLE_H = ColorAnalyzer_v2.N_OPS_H * TAG_WH,
            CHAR_WH = 6,
            CHAR_PAD = (TAG_WH - CHAR_WH) / 2,
            LABEL_PAD = 3,
            LABEL_WH = CHAR_WH + LABEL_PAD,
            TAG_BY_SCORE_W = TAG_WH + 2,
            TAG_BY_SCORE_H = TAG_WH + 2 * CHAR_WH + 2,
            TAG_TABLE_FULL_W = LABEL_WH + TAG_TABLE_W,
            TAG_TABLE_FULL_H = LABEL_WH + TAG_TABLE_H,
            W = IMAGE_WH + PROFILE_W + 3 * PAD,
            H = IMAGE_WH + 3 * TAG_BY_SCORE_H + LABEL_WH + 3 * PAD;

        const int // columns, rows
            C0 = PAD,
            C1 = C0 + IMAGE_WH + PAD,
            CW = PAD,
            C0R0 = PAD,
            C0R1 = C0R0 + IMAGE_WH + PAD,
            C1R0 = PAD,
            C1R1 = C1R0 + PROFILE_H + PAD,
            C1R2 = C1R1 + TAG_TABLE_FULL_H + PAD,
            CWR0 = C0R0 + IMAGE_WH + PAD;

        // todo  cw: legend + tags by score;  c1: tags count + tables

        var colorTextB = 160.ToRgb24();
        var colorText  = 120.ToRgb24();
        var colorTextD = 100.ToRgb24();
        var colorTagHl =  68.ToRgb24();
        var colorBack  =  64.ToRgb24();
        var colorNoTag =  48.ToRgb24();

        // ANALYZE IMAGE
        var tags = ColorTagger_v2.AnalyzeImage(path).Result.OrderByDescending(x => x.Score).ToArray();
        if (tags.Length == 0)
        {
            Print("NO TAGS, IMAGE EMPTY");
            return;
        }

        // REPORT
        using var source = Image.Load<Rgb24>(path);
        using var report = new Image<Rgb24>(W, H, colorBack);

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
                var x = xl + TAG_WH * hi;
                var y = yl + TAG_WH * oi;
                var key = $"{ColorTagger_v2.KEYS_HUE[hi]}{ColorTagger_v2.KEYS_OPT[oi]}";
                var i = ColorAnalyzer_v2.N_OPS_H * hi + oi;
                DrawTagSquare(x, y, key, () => _palette_H[i]);
            }

            // TAGS - ACHROMATIC
            for (var i = 0; i < ColorAnalyzer_v2.N_OPS_G; i++)
            {
                var x = xl + TAG_TABLE_W + PAD + LABEL_WH;
                var y = yl + i * TAG_WH;
                var key = $"{ColorTagger_v2.KEY_GRAY}{i}";
                var i_ = i;
                DrawTagSquare(x, y, key, () =>
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
                    if (score >= 10)
                        report.Mutate(ctx => ctx.Fill(colorTagHl, new RectangleF(x, y, TAG_WH, TAG_WH)));
                    report.Mutate(ctx => ctx.Fill(getColor(), GetTagSquareRect(x, y, score)));
                }
                else
                    report.DrawASCII("!", colorNoTag, new Point(x + CHAR_PAD + 2, y + CHAR_PAD));
            }
        }

        // TAGS BY SCORE
        {
            const string SML = "SML";
            const int
                x0 = CW,
                y0 = CWR0,
                xl = x0 + LABEL_WH,
                yl = y0 + LABEL_WH;

            var groups = tags
                .GroupBy(x => (int)Math.Log10(x.Score.Cap(9999)))
                .Take(3)
                .ToDictionary(x => x.Key, x => x.ToArray());
            var x = xl;
            var y = yl;
            foreach (var (key, tags_) in groups)
            {
                var key_SML = SML.AsSpan(key - 1, 1);
                report.DrawASCII(key_SML, colorText, new Point(x0, y + TAG_WH.GapInt(CHAR_WH)));
                foreach (var tag in tags_)
                {
                    if (x + TAG_BY_SCORE_W > W || y + TAG_BY_SCORE_H > H) break;

                    var (color, _) = GetColorsByTag(tag.Term);
                    DrawTagSquare_Labeled(x, y, tag.Term, tag.Score, () => color);
                    x += TAG_BY_SCORE_W;
                }

                x = xl;
                y += TAG_BY_SCORE_H;
            }

            var legend = "Score:  S: 10-99,  M: 100-999,  L: 1k-10k";
            report.DrawASCII(legend, colorText, new Point(x0, y0));

            void DrawTagSquare_Labeled(int x, int y, string key, int score, Func<Rgb24> getColor)
            {
                var y_key   = y + TAG_WH;
                var y_score = y_key + CHAR_WH;
                report.Mutate(ctx => ctx.Fill(getColor(), GetTagSquareRect(x, y, score)));
                report.DrawASCII(key, colorTextD, new Point(x + TAG_WH.GapInt(key.Length * CHAR_WH), y_key));
                if (score < 100)
                {
                    var text = $"{score}";
                    report.DrawASCII(text, colorText, new Point(x + TAG_WH.GapInt(text.Length * CHAR_WH), y_score));
                }
                else
                {
                    var hundreds  = $"{score / 100}";
                    var remainder = $"{score % 100:00}";
                    report.DrawASCII(hundreds, colorTextB, new Point(x, y_score));
                    var free_chars = 3 - hundreds.Length;
                    if (free_chars > 0)
                    {
                        var point = new Point(x + hundreds.Length * CHAR_WH, y_score);
                        report.DrawASCII(remainder.AsSpan(0, free_chars), colorText, point);
                    }
                }
            }
        }

        RectangleF GetTagSquareRect(int x, int y, int score)
        {
            var side = (float)(score * 0.016).FastPow(0.5);
            var gap = ((float)TAG_WH).Gap(side).RoundInt();
            return new RectangleF(x + gap, y + gap, side, side);
        }

        // OTHER INFO
        {
            var tagsCount_db = tags.TakeWhile(x => x.Score >= 10).Count();
            report.DrawASCII($"TAGS: {tags.Length,3} -> {tagsCount_db}", colorText, new Point(C1, C1R2));
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

                var (bg_rgb, fg_rgb) = GetColorsByTag(term);
                
                var bg = ColorConverter.RgbToHex(bg_rgb.ToRGB());
                var fg = ColorConverter.RgbToHex(fg_rgb.ToRGB());
                AnsiConsole.Markup($"[{fg} on #{bg}]\t {term,3} - {score,5} [/]");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    private static (Rgb24 bg, Rgb24 fg) GetColorsByTag(string term)
    {
        Rgb24 bg;
        var special = false;
        if      (term[0] == ColorTagger_v2.KEY_GRAY)
        {
            var L = ColorAnalyzer_v2.GrayReferences_L[term[1] - '0'];
            var l = (L * 100).RoundInt().Cap(100);
            bg = new HSL(0, 0, (byte)l).ToRgb24();
        }
        else if (term[0] == ColorTagger_v2.KEY_WEAK)
        {
            var hc = ColorTagger_v2.KEYS_HC.IndexOf(term[1]);
            var dl = ColorTagger_v2.KEYS_DL.IndexOf(term[2]);
            bg = _palette_W[2 * hc + dl];
        }
        else if (term[0] == ColorTagger_v2.KEY_MISC)
        {
            bg = 0.ToRgb24();
            special = true;
        }
        else
        {
            var hue_ix = ColorTagger_v2.KEYS_HUE.IndexOf(term[0]);
            var opt_ix = ColorTagger_v2.KEYS_OPT.IndexOf(term[1]);
            bg = _palette_H[6 * hue_ix + opt_ix];
        }

        var fg = special ? new Rgb24(255, 215, 0) : bg.ToOklch().L > 0.5 ? 0.ToRgb24() : 255.ToRgb24();

        return (bg, fg);
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
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
                AnsiConsole.Markup($"[{fg} on #{bg}]        [/]");
            }

            Console.WriteLine();
        }
    }

    // CONSTS

    private const int
        PAD = 6,
        IMAGE_WH = 394,
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

    private const int
        // X - columns
        C0 = PAD,
        C1 = C0 + IMAGE_WH + PAD,
        // Y - rows of columns
        C0R0 = PAD, // image
        C0R1 = C0R0 + IMAGE_WH + PAD,  // tags by score
        C1R0 = PAD, // oklch profile
        C1R1 = C1R0 + PROFILE_H + PAD, // tags by category
        C1R2 = C1R1 + TAG_TABLE_FULL_H + PAD; // TBA 

    private static readonly Rgb24
        colorTextB = 160.ToRgb24(), // bold
        colorText  = 120.ToRgb24(),
        colorTextD = 100.ToRgb24(), // dark
        colorTagHl =  68.ToRgb24(), // tag square highlight
        colorBack  =  64.ToRgb24(), // background
        colorNoTag =  48.ToRgb24(), // !
        colorTextS =  48.ToRgb24(); // shadow

    public static void Run(string path)
    {
        // ANALYZE IMAGE
        var sw = Stopwatch.StartNew();
        var analysis = ColorTagger_v2.AnalyzeImage(path, minScore: 1).Result;
        sw.Log("1 - ANALYSIS");

        var tags = analysis.OrderByDescending(x => x.Score).ToArray();
        if (tags.Length == 0)
        {
            Print("NO TAGS, IMAGE EMPTY");
            return;
        }

        // REPORT
        using var source = Image.Load<Rgba32>(path);
        using var report = new Image<Rgb24>(W, H, colorBack);
        var step = ColorTagService.CalculateStep(source.Size);

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
                var key = ColorTagger_v2.HUES_C0.AsSpan(hi, 1);
                var x = xl + hi * TAG_WH + CHAR_PAD;
                report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x, y0));
            }
            for (var oi = 0; oi < ColorAnalyzer_v2.N_OPS_H; oi++)
            {
                var key = ColorTagger_v2.HUES_C1.AsSpan(oi, 1);
                var y = yl + oi * TAG_WH + CHAR_PAD;
                report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x0, y));
            }

            // LABELS - ACHROMATIC
            {
                var key = ColorTagger_v2.GRAY_C0.AsSpan(0, 1);
                var x = x0 + TAG_TABLE_FULL_W + PAD + LABEL_WH + CHAR_PAD;
                report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x, y0));
            }
            for (var i = 0; i < ColorAnalyzer_v2.N_OPS_G; i++)
            {
                var key = ColorTagger_v2.GRAY_C1.AsSpan(i, 1);
                var x = x0 + TAG_TABLE_FULL_W + PAD;
                var y = yl + i * TAG_WH + CHAR_PAD;
                report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x, y));
            }

            // LABELS - GENERAL
            {
                var key = ColorTagger_v2.MISC_C0.AsSpan(0, 1);
                var x = x0 + TAG_TABLE_FULL_W + PAD + LABEL_WH + TAG_WH + PAD + LABEL_WH + CHAR_PAD;
                report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x, y0));
            }
            for (var i = 0; i < ColorAnalyzer_v2.N_GENERAL; i++)
            {
                var key = ColorTagger_v2.MISC_C1.AsSpan(i, 1);
                var x = x0 + TAG_TABLE_FULL_W + PAD + LABEL_WH + TAG_WH + PAD;
                var y = yl + i * TAG_WH + CHAR_PAD;
                report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x, y));
            }

            var tag_scores = tags.ToDictionary(x => x.Term, x => x.Score);

            // TAGS - CHROMATIC
            for (var oi = 0; oi < ColorAnalyzer_v2.N_OPS_H; oi++)
            for (var hi = 0; hi < ColorAnalyzer_v2.N_HUES;  hi++)
            {
                var x = xl + TAG_WH * hi;
                var y = yl + TAG_WH * oi;
                var key = $"{ColorTagger_v2.HUES_C0[hi]}{ColorTagger_v2.HUES_C1[oi]}";
                var score = tag_scores.GetValueOrDefault(key, 0);
                var i = ColorAnalyzer_v2.N_OPS_H * hi + oi;
                report.DrawTagSquare(x, y, score, () => _palette_H[i]);
            }

            // TAGS - ACHROMATIC
            for (var i = 0; i < ColorAnalyzer_v2.N_OPS_G; i++)
            {
                var x = xl + TAG_TABLE_W + PAD + LABEL_WH;
                var y = yl + i * TAG_WH;
                var key = $"{ColorTagger_v2.GRAY_C0}{ColorTagger_v2.GRAY_C1[i]}";
                var score = tag_scores.GetValueOrDefault(key, 0);
                var i_ = i;
                report.DrawTagSquare(x, y, score, () =>
                {
                    var L = ColorAnalyzer_v2.GrayReferences_L[i_];
                    var l = (L * 100).RoundInt().Cap(100);
                    return new HSL(0, 0, (byte)l).ToRgb24();
                });
            }

            // TAGS - GENERAL
            for (var i = 0; i < ColorAnalyzer_v2.N_GENERAL; i++)
            {
                var x = xl + TAG_TABLE_W + PAD + LABEL_WH + TAG_WH + PAD + LABEL_WH;
                var y = yl + i * TAG_WH;
                var key = $"{ColorTagger_v2.MISC_C0}{ColorTagger_v2.MISC_C1[i]}";
                var score = tag_scores.GetValueOrDefault(key, 0);
                var color = GetColorExtra(key);
                report.DrawTagSquare(x, y, score, () => color, hatch: true, proportional: true);
            }
        }

        // TAGS BY SCORE
        {
            const string SML = "_SML";
            const int
                x0 = C0,
                y0 = C0R1,
                xl = x0 + LABEL_WH,
                yl = y0 + LABEL_WH;

            var groups = tags
                .Where(x => x.Term.StartsWith('#').Janai())
                .GroupBy(x => (int)Math.Log10(x.Score.Cap(9999)))
                .Where(g => g.Key > 0)
                .ToDictionary(x => x.Key, x => x.ToArray());
            var x = xl;
            var y = yl;
            for (var i = SML.Length - 1; i > 0; i--)
            {
                if (groups.TryGetValue(i, out var tags_i))
                {
                    var key = SML.AsSpan(i, 1);
                    report.DrawASCII_Shady(key, colorText, colorTextS, new Point(x0, y + TAG_WH.GapInt(CHAR_WH)));
                    foreach (var tag in tags_i)
                    {
                        if (x + TAG_BY_SCORE_W > W || y + TAG_BY_SCORE_H > H) break;

                        var color = GetColorByTag(tag.Term);
                        report.DrawTagSquare_Labeled(x, y, tag, () => color);
                        x += TAG_BY_SCORE_W;
                    }
                }

                x = xl;
                y += TAG_BY_SCORE_H;
            }

            // GENERAL TAGS
            {
                x = C1   + LABEL_WH;
                y = C0R1 + LABEL_WH;
                report.DrawASCII_Shady("#", colorText, colorTextS, new Point(C1, y + TAG_WH.GapInt(CHAR_WH)));
                var tags_x = tags
                    .Where(t => t.Term.StartsWith(ColorTagger_v2.MISC_C0) && t.Score >= 10)
                    .OrderByDescending(t => t.Score);
                foreach (var tag in tags_x)
                {
                    if (x + TAG_BY_SCORE_W > W || y + TAG_BY_SCORE_H > H) break;

                    var color = GetColorExtra(tag.Term);
                    report.DrawTagSquare_Labeled(x, y, tag, () => color, hatch: true, proportional: true);
                    x += TAG_BY_SCORE_W;
                }
            }

            // TAGS COUNT BY SCORE
            {
                var L = groups.TryGetValue(3, out var a3) ? a3.Length : 0;
                var M = groups.TryGetValue(2, out var a2) ? a2.Length : 0;
                var S = groups.TryGetValue(1, out var a1) ? a1.Length : 0;
                var p = new Point(x0, y0);
                p = report.DrawASCII_Shady("TAGS BY SCORE: ", colorText,  colorTextS, p);
                p = report.DrawASCII_Shady($"{L,3}",          colorTextB, colorTextS, p);
                p = report.DrawASCII_Shady("*L (1k-10k), ",   colorText,  colorTextS, p);
                p = report.DrawASCII_Shady($"{M,3}",          colorTextB, colorTextS, p);
                p = report.DrawASCII_Shady("*M (100-999), ",  colorText,  colorTextS, p);
                p = report.DrawASCII_Shady($"{S,3}",          colorTextB, colorTextS, p);
                _ = report.DrawASCII_Shady("*S (10-99)",      colorText,  colorTextS, p);
            }
        }

        // TAGS COUNT
        {
            var p = new Point(C1, C0R1);
            var tagsCount_db = tags.TakeWhile(x => x.Score >= 10).Count();
            p = report.DrawASCII_Shady("TAGS: ",             colorText,  colorTextS, p);
            p = report.DrawASCII_Shady($"{tagsCount_db,2}",  colorTextB, colorTextS, p);
            _ = report.DrawASCII_Shady($"/{tags.Length,2}",  colorText,  colorTextS, p);
            p.X = (C1 + W - PAD) / 2;
            p = report.DrawASCII_Shady("STEP: ",             colorText,  colorTextS, p);
            p = report.DrawASCII_Shady($"{step,2}",          colorTextB, colorTextS, p);
            p = report.DrawASCII_Shady("  SIZE: ",           colorText,  colorTextS, p);
            p = report.DrawASCII_Shady($"{source.Width,4}",  colorTextB, colorTextS, p);
            p = report.DrawASCII_Shady("*",                  colorText,  colorTextS, p);
            _ = report.DrawASCII_Shady($"{source.Height,4}", colorTextB, colorTextS, p);
        }

        // OKLCH v2 PROFILE
        using var profile = DebugTools.GetReportBackground_Oklch(useMagenta: false);
        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y].Rgb;
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

        sw.Log("2 - REPORT");

        // SAVE REPORT
        var name = $"Analysis-{Desert.Clock():x}-{Desert.GetSand()}.png";
        var save = Dir_Debug_Mixed.EnsureDirectoryExist().Combine(name);
        report.SaveAsPng(save);

        sw.Log("3 - SAVE");
    }

    // DRAW TAG SQUARE

    private static void DrawTagSquare
    (
        this Image<Rgb24> report, int x, int y,
        int score,
        Func<Rgb24> getColor,
        bool hatch = false, bool proportional = false
    )
    {
        if (score > 0)
        {
            var highlight = score >= 10;
            if (highlight)
                report.Mutate(ctx => ctx.Fill(colorTagHl, new RectangleF(x, y, TAG_WH, TAG_WH)));
            report.Mutate(ctx => ctx.Fill(getColor(), GetTagSquareRect(x, y, score, proportional)));
            if (hatch)
            {
                var color = highlight ? colorTagHl : colorBack;
                for (var iy = y; iy < y + TAG_WH; iy++)
                for (var ix = x; ix < x + TAG_WH; ix++)
                {
                    if (ix.IsEven() == iy.IsEven())
                        report[ix, iy] = color;
                }
            }
        }
        else
            report.DrawASCII("!", colorNoTag, new Point(x + CHAR_PAD + 2, y + CHAR_PAD));
    }

    private static void DrawTagSquare_Labeled
    (
        this Image<Rgb24> report, int x, int y,
        TagContent tag,
        Func<Rgb24> getColor,
        bool hatch = false, bool proportional = false
    )
    {
        var (key, score) = tag;
        var y_key   = y + TAG_WH;
        var y_score = y_key + CHAR_WH;
        report.Mutate(ctx => ctx.Fill(getColor(), GetTagSquareRect(x, y, score, proportional)));
        if (hatch)
        {
            for (var iy = y; iy < y + TAG_WH; iy++)
            for (var ix = x; ix < x + TAG_WH; ix++)
            {
                if (ix.IsEven() == iy.IsEven())
                    report[ix, iy] = colorBack;
            }
        }
        report.DrawASCII_Shady(key, colorTextD, colorTextS, new Point(x + TAG_WH.GapInt(key.Length * CHAR_WH), y_key));
        if (score < 100)
        {
            var text = $"{score}";
            report.DrawASCII_Shady(text, colorText, colorTextS, new Point(x + TAG_WH.GapInt(text.Length * CHAR_WH), y_score));
        }
        else
        {
            var hundreds  = $"{score / 100}";
            var remainder = $"{score % 100:00}";
            report.DrawASCII_Shady(hundreds, colorTextB, colorTextS, new Point(x, y_score));
            var free_chars = 3 - hundreds.Length;
            if (free_chars > 0)
            {
                var point = new Point(x + hundreds.Length * CHAR_WH, y_score);
                report.DrawASCII_Shady(remainder.AsSpan(0, free_chars), colorText, colorTextS, point);
            }
        }
    }

    private static RectangleF GetTagSquareRect
        (int x, int y, int score, bool proportional = false)
    {
        var side = proportional
            ? // REAL, area ~ score
            (float)(score * 0.0256).FastPow(0.5)
            : // INFORMATIVE, makes square bigger for easier visual assessment 
            (float)Math.Log10(score).FastPow(2);

        var gap = ((float)TAG_WH).Gap(side).RoundInt();
        return new RectangleF(x + gap, y + gap, side, side);
    }

    // PALETTE / GET COLOR

    private static Rgb24 GetColorByTag(string term)
    {
        if      (term.StartsWith(ColorTagger_v2.GRAY_C0))
        {
            var L = ColorAnalyzer_v2.GrayReferences_L[term[1] - '0'];
            var l = (L * 100).RoundInt().Cap(100);
            return new HSL(0, 0, (byte)l).ToRgb24();
        }
        else if (term.StartsWith(ColorTagger_v2.MISC_C0))
        {
            return 0.ToRgb24();
        }
        else
        {
            var hue_ix = ColorTagger_v2.HUES_C0.IndexOf(term[0]);
            var opt_ix = ColorTagger_v2.HUES_C1.IndexOf(term[1]);
            return _palette_H[ColorAnalyzer_v2.N_OPS_H * hue_ix + opt_ix];
        }
    }

    private static Rgb24 GetColorExtra(string term) => term[1] switch
    {
        'X' => 128.ToRgb24(),
        'A' => 128.ToRgb24(),
        'S' => new HSL(120, 100, 50).ToRgb24(),
        'P' => new HSL(120,  25, 50).ToRgb24(),
        'D' =>  32.ToRgb24(),
        'L' => 196.ToRgb24(),
        _   => SixLabors.ImageSharp.Color.Magenta,
    };

    private static readonly Rgb24[]
        _palette_H = GeneratePalette_Hue();

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
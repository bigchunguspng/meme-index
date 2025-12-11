using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color.v2;

public static class ColorTagger_v2
{
    public const int MAX_SCORE = 10_000;

    public static async Task<IEnumerable<TagContent>> AnalyzeImage
        (string path, int minScore = 10)
    {
        var sw = Stopwatch.StartNew();

        using var image = await Image.LoadAsync<Rgba32>(path);
        sw.LogCM(ConsoleColor.Yellow, "LOAD");

        var report = ColorAnalyzer_v2.ScanImage(image);
        sw.LogCM(ConsoleColor.Yellow, "SCAN");

        var tags = AnalyzeImageScan(report, minScore);
        sw.LogCM(ConsoleColor.Yellow, "ANALYZE");

        return tags
            .Select(x => new TagContent(x.Key, x.Value));
    }

    /// Returns a raw numbers for all possible tags.
    private static Dictionary<string, int> AnalyzeImageScan
        (ImageScanReport_v22 report, int minScore = 10)
    {
        var tags = new Dictionary<string, int>();

        var samplesTotal  = report.SamplesTotal;
        var samplesOpaque = report.SamplesOpaque;
        var opacityTotal  = report.OpacityTotal;

        // GRAY
        for (var i = 0; i < ColorAnalyzer_v2.N_OPS_G; i++)
        {
            AddTag(_tags_A[i], GetRawScore_Opaque(report.Scores_Gray[i]));
        }

        // BY HUE
        var hues = ColorAnalyzer_v2.N_HUES.Times(_ => new ImageScan_Hue_v22());
        for (var bi = 0; bi < ColorAnalyzer_v2.B_HUES; bi++)
        {
            // primary + vague -> primary

            var hue_ix = bi >> 1;
            var samples = report.Hues[bi];
            var primary = bi.IsEven();
            if (primary)
            {
                hues[hue_ix].Combine(samples, multiplier: 1.0);
            }
            else
            {
                // split samples between primary hues to the sides
                var  hue_i2 = (hue_ix + 1) % ColorAnalyzer_v2.N_HUES;
                hues[hue_ix].Combine(samples, multiplier: 0.5);
                hues[hue_i2].Combine(samples, multiplier: 0.5);
            }
        }

        for (var hi = 0; hi < ColorAnalyzer_v2.N_HUES; hi++)
        {
            // struct -> tags

            var hue = hues[hi];
            var hue_offset = hi * ColorAnalyzer_v2.N_OPS_H;
            for (var o = 0; o < ColorAnalyzer_v2.N_OPS_H; o++)
            {
                AddTag(_tags_H[hue_offset + o], GetRawScore_Opaque(hue[o]));
            }
        }

        // GENERAL
        var transparent = MAX_SCORE - MAX_SCORE * opacityTotal / (samplesTotal * 255);
        AddTag(_tags_X[0], (int)transparent);

        AddTag(_tags_X[1], GetRawScore_Opaque(report.Gray));
        AddTag(_tags_X[2], GetRawScore_Opaque(report.Bold));
        AddTag(_tags_X[3], GetRawScore_Opaque(report.Pale));
        AddTag(_tags_X[4], GetRawScore_Opaque(report.Dark));
        AddTag(_tags_X[5], GetRawScore_Opaque(report.Light));

        return tags;

        // ==

        [MethodImpl(AggressiveInlining)]
        void AddTag(string term, int score)
        {
            if (score >= minScore) tags.Add(term, score);
        }

        [MethodImpl(AggressiveInlining)]
        int GetRawScore_Opaque
            (double value) // value: color-tag score: 0..samplesOpaque
            => (MAX_SCORE * value / samplesOpaque).RoundInt();
    }

    public const string
        HUES_C0 = "ROYLGCSBVP",
        GRAY_C0 = "A",
        MISC_C0 = "#",
        HUES_C1 = "SPDL01", // RS RP RD RL R0 R1
        MISC_C1 = "XASPDL", // #X, #A, #S, …    Misc, general, extra.
        GRAY_C1 = "01234";  // A0 - A4          Gray, achromatic, luma.

    private static readonly string[]
        _tags_A = GetTagCombinations(GRAY_C0, GRAY_C1),
        _tags_H = GetTagCombinations(HUES_C0, HUES_C1),
        _tags_X = GetTagCombinations(MISC_C0, MISC_C1);

    private static string[] GetTagCombinations
        (ReadOnlySpan<char> rows, ReadOnlySpan<char> cols)
    {
        var w = cols.Length;
        var h = rows.Length;
        var array = new string[w * h];
        for (var ih = 0; ih < h; ih++)
        for (var iw = 0; iw < w; iw++)
        {
            array[iw + w * ih] = $"{rows[ih]}{cols[iw]}";
        }

        return array;
    }
}
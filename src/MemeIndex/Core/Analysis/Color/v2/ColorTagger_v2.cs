using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color.v2;

public static class ColorTagger_v2
{
    public const int MAX_SCORE = 10_000;

    public static async Task<IEnumerable<TagContent>> AnalyzeImage(string path)
    {
        var sw = Stopwatch.StartNew();

        using var image = await Image.LoadAsync<Rgba32>(path);
        sw.LogCM(ConsoleColor.Yellow, "LOAD");

        var report = ColorAnalyzer_v2.ScanImage(image);
        sw.LogCM(ConsoleColor.Yellow, "SCAN");

        var tags = AnalyzeImageScan(report);
        sw.LogCM(ConsoleColor.Yellow, "ANALYZE");

        return tags
            .Where(x => x.Value > 0)
            //.Where(x => x.Value > 100)
            // todo math to make tag scores logarithmic?
            .Select(x => new TagContent(x.Key, x.Value));
    }

    /// Returns a raw numbers for all possible tags.
    private static Dictionary<string, int> AnalyzeImageScan(ImageScanReport report)
    {
        var tags = new Dictionary<string, int>();

        var samplesTotal  = report.SamplesTotal;
        var samplesOpaque = report.SamplesOpaque;
        var opacityTotal  = report.OpacityTotal;

        // GRAY
        var lenG = report.Grays.Length;
        for (var i = 0; i < lenG; i++)
        {
            tags.Add(_tags_Y[i], GetRawScore_Opaque(report.Grays[i]));
        }

        // WEAK
        {
            tags.Add(_tags_W[0], GetRawScore_Opaque(report.WeakHot_D));
            tags.Add(_tags_W[1], GetRawScore_Opaque(report.WeakHot_L));
            tags.Add(_tags_W[2], GetRawScore_Opaque(report.WeakCoolD));
            tags.Add(_tags_W[3], GetRawScore_Opaque(report.WeakCoolL));
        }

        // BY HUE
        var hues = new ImageScan_Hue[ColorAnalyzer_v2.N_HUES];
        var lenH = report.Hues.Length;
        for (var i = 0; i < lenH; i++)
        {
            // primary + vague -> primary

            var hue_ix = i >> 1;
            var samples = report.Hues[i];
            var primary = i.IsEven();
            if (primary)
            {
                hues[hue_ix].Combine(samples, multiplier: 2);
            }
            else
            {
                // split samples between primary hues to the sides
                var  hue_i2 = (hue_ix + 1) % ColorAnalyzer_v2.N_HUES;
                hues[hue_ix].Combine(samples, multiplier: 1);
                hues[hue_i2].Combine(samples, multiplier: 1);
            }
        }

        for (var i = 0; i < ColorAnalyzer_v2.N_HUES; i++)
        {
            // struct -> tags

            var hue = hues[i];
            var hue_offset = i * 6;
            tags.Add(_tags_H[hue_offset + 0], GetRawScore_Opaque(hue.Bold));
            tags.Add(_tags_H[hue_offset + 1], GetRawScore_Opaque(hue.Pale));
            tags.Add(_tags_H[hue_offset + 2], GetRawScore_Opaque(hue.Dark));
            tags.Add(_tags_H[hue_offset + 3], GetRawScore_Opaque(hue.Light));
            tags.Add(_tags_H[hue_offset + 4], GetRawScore_Opaque(hue.PaleXD));
            tags.Add(_tags_H[hue_offset + 5], GetRawScore_Opaque(hue.PaleXL));
        }

        // GENERAL - opacity, saturated/pale/gray, light/dark
        var transparent = MAX_SCORE - MAX_SCORE * opacityTotal / (samplesTotal * 255);
        tags.Add(_tags_X[0], (int)transparent);

        var gray  = report.Grays.Sum();
        var bold  = report.Hues.Sum(hue => hue.Bold);
        var pale  = report.Hues.Sum(hue => hue.Pale);
        var dark  = report.Hues.Sum(hue => hue.Dark)  + report.Grays.Take(3).Sum();
        var light = report.Hues.Sum(hue => hue.Light) + report.Grays.Skip(3).Sum();

        tags.Add(_tags_X[1], GetRawScore_General(gray));
        tags.Add(_tags_X[2], GetRawScore_General(bold));
        tags.Add(_tags_X[3], GetRawScore_General(pale));
        tags.Add(_tags_X[4], GetRawScore_General(dark));
        tags.Add(_tags_X[5], GetRawScore_General(light));

        return tags;

        // ==

        [MethodImpl(AggressiveInlining)]
        int GetRawScore_Opaque  (int value) => MAX_SCORE * value / samplesOpaque;

        [MethodImpl(AggressiveInlining)]
        int GetRawScore_General (int value)
        {
            var ratio = (double) value / samplesOpaque;
            return ratio < 0.25
                ? 0
                : (MAX_SCORE * 0.0001.FastPow(1 - ratio)).RoundInt();
        }
    }

    public const string
        KEYS_HUE = "ROYLGCSBVP",
        KEYS_OPT = "SPDL01", // RS RP RD RL R0 R1
        KEYS_HC = "HC",
        KEYS_DL = "DL";

    public const char
        KEY_GRAY = '_', // _0 - _5
        KEY_MISC = '#', // #D, #S, #X, …
        KEY_WEAK = 'W'; // WHD - WCL

    private static readonly string[]
        _tags_Y = ["_0", "_1", "_2", "_3", "_4", "_5"], // Luma (Y)
        _tags_H = // Only hue full!
        [
            "RS", "RP", "RD", "RL", "R0", "R1",
            "OS", "OP", "OD", "OL", "O0", "O1",
            "YS", "YP", "YD", "YL", "Y0", "Y1",
            "LS", "LP", "LD", "LL", "L0", "L1",
            "GS", "GP", "GD", "GL", "G0", "G1",
            "CS", "CP", "CD", "CL", "C0", "C1",
            "SS", "SP", "SD", "SL", "S0", "S1",
            "BS", "BP", "BD", "BL", "B0", "B1",
            "VS", "VP", "VD", "VL", "V0", "V1",
            "PS", "PP", "PD", "PL", "P0", "P1",
        ],
        _tags_W = ["WHD", "WHL", "WCD", "WCL"], // Weak
        _tags_X = ["#X", "#Y", "#S", "#P", "#D", "#L"]; // Extra
}
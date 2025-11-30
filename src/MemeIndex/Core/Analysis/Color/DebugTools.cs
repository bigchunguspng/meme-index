using MemeIndex.Core.Analysis.Color.v1;
using MemeIndex.Core.Analysis.Color.v2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex.Core.Analysis.Color;

public static partial class DebugTools
{
    public static readonly JpegEncoder JpegEncoder_Q80 = new() { Quality = 80 };

    public static void Test()
    {
        RenderHues_Oklch_v2();
        //ColorProfile.RenderHues(); // 12s -> 7s
        Log("DONE");
        return;
        ColorProfile.GeneratePalette_Saturated();
        return;
    }

    public static void RenderAllProfiles(string path)
    {
        var sw = Stopwatch.StartNew();

        /*RenderProfile_HSL(path);
        times[0] += sw.Elapsed;
        sw.LogCM(ConsoleColor.Yellow, "\tProfile - HSL");*/

        RenderProfile_Oklch(path);
        sw.LogCM(ConsoleColor.Yellow, "\tProfile - Oklch");

        RenderProfile_Oklch_v2(path);
        sw.LogCM(ConsoleColor.Yellow, "\tProfile - Oklch-v2");

        /*RenderSamplePoster(path);
        sw.LogCM(ConsoleColor.Yellow, "\tPoster - Color v2");*/

        /*RenderProfile_Oklch_HxL(path);
        sw.LogCM(ConsoleColor.Yellow, "\tProfile - Oklch HxL");*/
    }

    // REPORT BACKGROUNDS

    private const int
        SIDE     = 101,
        SIDE_Hue = 361;

    private static Image<Rgb24> GetReportBackground_HxL()
    {
        var report = new Image<Rgb24>(SIDE_Hue, SIDE, 50.ToRgb24());

        var color = 40.ToRgb24();
        for (var hue_ix = 0; hue_ix < 12; hue_ix += 2)
        {
            const int w = ColorSearchProfile.HUE_RANGE_deg;
            var rect = new RectangleF(hue_ix * w, 0, w, SIDE_Hue);
            report.Mutate(x => x.Fill(color, rect));
        }

        return report;
    }

    private static Image<Rgb24> GetReportBackground_Oklch(bool useMagenta = true)
    {
        Rgb24 
            baseA = 50.ToRgb24(), baseB = 40.ToRgb24(),
            rectA = 54.ToRgb24(), rectB = 44.ToRgb24(),
            textA = 25.ToRgb24(), textB = 65.ToRgb24();
        var report = new Image<Rgb24>(SIDE * 3, SIDE * 2, baseA);

        for (var i = 0; i < 6; i++)
        {
            var row = i / 3;
            var col = i % 3;
            var odd = i.IsOdd();
            if (odd) report.Mutate(ctx => ctx.Fill(baseB, new RectangleF(col * SIDE, row * SIDE, SIDE, SIDE)));

            var c1 = odd ? baseB : baseA;
            var c2 = odd ? rectB : rectA;

            var offsetCol = col * SIDE;
            var offsetRow = row * SIDE;
            report.SwitchColors(c1, c2, new Rectangle(offsetCol, offsetRow + 71, SIDE, 30)); // Pale
            report.SwitchColors(c1, c2, new Rectangle(offsetCol, offsetRow + 86, SIDE,  9)); // Weak
            report.SwitchColors(c1, c2, new Rectangle(offsetCol + 50, offsetRow, 51, SIDE)); // Light
        }

        report.DrawPixelArt(ASCII.HUE_LABEL_01, textA, new Point(0 * SIDE + 2, 0 * SIDE + 2));
        report.DrawPixelArt(ASCII.HUE_LABEL_02, textA, new Point(0 * SIDE + 2, 0 * SIDE + 9));
        report.DrawPixelArt(ASCII.HUE_LABEL_03, textB, new Point(0 * SIDE + 2, 1 * SIDE + 2));
        report.DrawPixelArt(ASCII.HUE_LABEL_04, textB, new Point(0 * SIDE + 2, 1 * SIDE + 9));
        report.DrawPixelArt(ASCII.HUE_LABEL_05, textB, new Point(1 * SIDE + 2, 0 * SIDE + 2));
        report.DrawPixelArt(ASCII.HUE_LABEL_06, textB, new Point(1 * SIDE + 2, 0 * SIDE + 9));
        report.DrawPixelArt(ASCII.HUE_LABEL_07, textA, new Point(1 * SIDE + 2, 1 * SIDE + 2));
        report.DrawPixelArt(ASCII.HUE_LABEL_08, textA, new Point(1 * SIDE + 2, 1 * SIDE + 9));
        report.DrawPixelArt(ASCII.HUE_LABEL_09, textA, new Point(2 * SIDE + 2, 0 * SIDE + 2));
        if (useMagenta)
        {
            report.DrawPixelArt(ASCII.HUE_LABEL_10, textA, new Point(2 * SIDE + 2, 0 * SIDE + 9));
            report.DrawPixelArt(ASCII.HUE_LABEL_11, textB, new Point(2 * SIDE + 2, 1 * SIDE + 2));
        }
        else
            report.DrawPixelArt(ASCII.HUE_LABEL_10, textB, new Point(2 * SIDE + 2, 1 * SIDE + 2));

        return report;
    }

    private static Image<Rgb24> GetReportBackground_HSL()
    {
        var report = new Image<Rgb24>(SIDE * 3, SIDE * 2, 50.ToRgb24());

        for (var row = 0; row < 2; row++)
        for (var col = 0; col < 3; col++)
        {
            HSL_Report_PutLines(report, col * SIDE, row * SIDE);
        }

        return report;
    }

    private static void HSL_Report_PutLines
        (Image<Rgb24> image, int offsetX = 0, int offsetY = 0)
    {
        Rgb24 c1A = 98.ToRgb24(), c2A = 90.ToRgb24(), c3A = 80.ToRgb24(), c4A = 70.ToRgb24();
        Rgb24 c1B = 32.ToRgb24(), c2B = 40.ToRgb24(), c3B = 50.ToRgb24(), c4B = 60.ToRgb24();

        image.Mutate(x => x.Fill(c2A, new RectangleF(offsetX +  0, offsetY +  0, SIDE, 50))); // Y-DARK
        image.Mutate(x => x.Fill(c2B, new RectangleF(offsetX +  0, offsetY + 50, SIDE, 51))); // Y-LIGHT
        image.Mutate(x => x.Fill(c1A, new RectangleF(offsetX +  0, offsetY +  0, SIDE,  4))); // BLACK
        image.Mutate(x => x.Fill(c1B, new RectangleF(offsetX +  0, offsetY + 97, SIDE,  4))); // WHITE

        var b1 = ColorTagService.GetGrayscaleLimits();
        var b2 = ColorTagService.GetSubPaleLimits();
        var b3 = ColorTagService.GetPaleLimits();
        var b4 = ColorTagService.GetPeakLimits();
        for (var y = 4; y <= 96; y++)
        {
            var c1 = y < 50 ? c4A : c4B; // pale-weak
            var c2 = y < 50 ? c3A : c3B; // pale-strong
            var c3 = y < 50 ? c4B : c4A; // vivid

            var w1 = b1[y];
            var w2 = b2[y];
            var w3 = b3[y];
            var w4 = b4[y];
            var w5 = Math.Max(w1, w4);
            var w6 = Math.Max(w2, w4);

            DrawLine(y, c1, w1,   w2 - w1);
            DrawLine(y, c2, w2,   w3 - w2);
            DrawLine(y, c3, w3, SIDE - w3);
            DrawLine(y, c2, w5,   w3 - w5);
            DrawLine(y, c1, w6,   w3 - w6);
        }

        return;

        void DrawLine(int y, SixLabors.ImageSharp.Color color, int offset, int width)
        {
            image.Mutate(x => x.Fill(color, new RectangleF(offsetX + offset, offsetY + y, width, 1)));
        }
    }
}
namespace MemeIndex.Core.Analysis.Color.v2;

/* ===============  Legend  =============== *\

  D | Dark
  L | Light
  X | Extra
  N | Neutral

  Bold -       high chroma
  Pale -       low  chroma
  Weak - extra low  chroma (subset of ^)

  Primary hues: Red, Orange, Yellow, …
  Vague   hues: Red/Orange, Orange/Yellow, …

\* ======================================== */

public class ColorAnalysisReport
{
    // Buckets
    public readonly long[] Grays = new long[6];

    public long
        WeakHot_D, WeakHot_L,
        WeakCoolD, WeakCoolL;

    /// Contains primary hues and their combinations.
    /// Red, R/O, Orange, …, Pink, P/R.
    public readonly ColorAnalysis_Hue[] Hues
        =  new      ColorAnalysis_Hue[ColorAnalyzer_v2.B_HUES];
}

public struct ColorAnalysis_Hue
{
    public long
        BoldD, PaleD, PaleXD,
        BoldL, PaleL, PaleXL;
}

public enum Weak : byte
{
    WeakHot_D,
    WeakHot_L,
    WeakCoolD,
    WeakCoolL,
}
public enum Shade : byte
{
    BLACK,
    GRAY_XD,
    GRAY_D,
    GRAY_L,
    GRAY_XL,
    WHITE,
}
public enum Hue : byte
{
    RED,
    ORANGE,
    YELLOW,
    LIME,
    GREEN,
    CYAN,
    SKY,
    BLUE,
    VIOLET,
    PINK,
}

public readonly struct ColorComponentBoundary(double A, double B)
{
    public bool Matches(double value) => value >= A && value <= B;
}

public static class ColorAnalyzer_v2
{
    public const int
        N_HUES = 10,
        B_HUES = 20;

    public static readonly double[] u_limits_H = // hue upper limits
    [
          8, // PINK, i: 0
         20, // PINK-RED
         25, // RED
         40, // RED-ORANGE
         75, // ORANGE
         90, // ORANGE-YELLOW
        105, // YELLOW
        110, // YELLOW-LIME
        120, // LIME
        137, // LIME-GREEN
        150, // GREEN
        170, // GREEN-CYAN
        175, // CYAN
        210, // CYAN-SKY
        250, // SKY
        265, // SKY-BLUE
        275, // BLUE
        285, // BLUE-VIOLET
        315, // VIOLET
        325, // VIOLET-PINK
        360, // PINK x2, i: 20
    ];

    /// Writes to span a pair of red-based primary hue space indices (0..10).
    /// Second one is -1 if color matches to a single hue.
    public static void GetHueIndices(Oklch color, ref Span<int> hue_ixs)
    {
        if (color.H.IsNaN()) return;

        for (var i = 0; i <= B_HUES; i++)
        {
            if (color.H <= u_limits_H[i])
            {
                var hi = (i / 2 - 1 + N_HUES) % N_HUES;
                hue_ixs[0] = hi;
                hue_ixs[1] = i.IsEven() ? -1 : (hi + 1) % N_HUES;
                break;
            }
        }
    }

    public static void CategorizeColor(this ColorAnalysisReport report, Oklch color)
    {
        if (color.H.IsNaN() || color.C < 0.02)
            report.PutGray(color);
        else
        {
            if (color.C < 0.05)
                report.PutWeak(color);

            for (var i = 0; i <= B_HUES; i++)
            {
                if (color.H <= u_limits_H[i])
                {
                    // Red: i=2 -> bi=0  |  i: 0..20 -> bi: 18,19,0..19
                    var bi = (i - 2 + B_HUES) % B_HUES; // bucket index
                    report.Hues[bi].Put(color);
                    break;
                }
            }
        }
    }

    public static void PutGray
        (this ColorAnalysisReport report, Oklch color)
    {
        var gix = Math.Floor(color.L * 6).RoundInt();
        report.Grays[gix]++;
    }

    public static void PutWeak
        (this ColorAnalysisReport report, Oklch color)
    {
        if (color.H is > 110 and < 315)
        {
            if (color.L < 0.5)
                report.WeakCoolD++;
            else
                report.WeakCoolL++;
        }
        else
        {
            if (color.L < 0.5)
                report.WeakHot_D++;
            else
                report.WeakHot_L++;
        }
    }

    public static void Put
        (ref this ColorAnalysis_Hue report, Oklch color)
    {
        if (color.C > 0.10)
        {
            if (color.L > 0.5)
                report.BoldL++;
            else
                report.BoldD++;
        }
        else
        {
            if      (color.L < 0.3)
                report.PaleXD++;
            else if (color.L < 0.5)
                report.PaleD++;
            else if (color.L < 0.7)
                report.PaleL++;
            else
                report.PaleXL++;
        }
    }

}
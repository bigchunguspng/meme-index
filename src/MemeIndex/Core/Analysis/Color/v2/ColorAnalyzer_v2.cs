using ColorHelper;
using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

public class ImageScanReport
{
    public int
        SamplesTotal  = 0, // Say 10k
        SamplesOpaque = 0; // < ^

    public long
        OpacityTotal  = 0; // up to 255 * ^^ = 2.5M

    // Values in each bucket: < SamplesTotal

    /// Black, 4 shades of gray, White.
    public readonly int[] Grays = new int[6];

    public int
        WeakHot_D, WeakHot_L,
        WeakCoolD, WeakCoolL;

    /// Contains primary hues and their combinations.
    /// Red, R/O, Orange, …, Pink, P/R.
    public readonly ImageScan_Hue[] Hues
        =  new      ImageScan_Hue[ColorAnalyzer_v2.B_HUES];
}

public struct ImageScan_Hue
{
    public int
        BoldD, PaleD, PaleXD,
        BoldL, PaleL, PaleXL;

    /*public double
        MaxL, MinL,
        MaxC, MinC;*/

    public int Dark  => BoldD + PaleD + PaleXD;
    public int Light => BoldL + PaleL + PaleXL;
    public int Bold  => BoldD + BoldL;
    public int Pale  => PaleD + PaleL + PaleXD + PaleXL;

    public void Combine(ImageScan_Hue other, int multiplier)
    {
        BoldD  += multiplier * other.BoldD;
        PaleD  += multiplier * other.PaleD;
        PaleXD += multiplier * other.PaleXD;
        BoldL  += multiplier * other.BoldL;
        PaleL  += multiplier * other.PaleL;
        PaleXL += multiplier * other.PaleXL;
    }
}

public enum Weak : byte
{
    WeakHot_D, WeakHot_L,
    WeakCoolD, WeakCoolL,
}
public enum Shade : byte
{
    BLACK, GRAY_XD, GRAY_D,
    GRAY_L, GRAY_XL, WHITE,
}
public enum Hue : byte
{
    RED,    ORANGE, YELLOW, LIME,
    GREEN,  CYAN,   SKY,    BLUE,
    VIOLET, PINK,
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
         15, // PINK, i: 0
         18, // PINK-RED
         30, // RED
         35, // RED-ORANGE
         83, // ORANGE
         90, // ORANGE-YELLOW
        110, // YELLOW
        113, // YELLOW-LIME
        125, // LIME
        132, // LIME-GREEN
        148, // GREEN
        156, // GREEN-CYAN
        180, // CYAN
        194, // CYAN-SKY
        245, // SKY
        263, // SKY-BLUE
        282, // BLUE
        285, // BLUE-VIOLET
        315, // VIOLET
        329, // VIOLET-PINK
        360, // PINK x2, i: 20
    ];

    // DEBUG

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

    // ACTUAL

    /// Color analysis PART I <br/>
    /// Collect color samples, categorize them, count.
    public static ImageScanReport ScanImage(Image<Rgba32> image)
    {
        var report = new ImageScanReport();

        var size = image.Size;
        var step = CalculateIteratorStep(size);

        Log($"step = {step}");

        // SCAN
        int total = 0, opaque = 0, opacity = 0;
        foreach (var (x, y) in new SizeIterator_45deg(size, step))
        {
            var sample = image[x, y];
            total++;
            opacity += sample.A;

            if (sample.A < 8) continue; // discard almost invisible pixels

            opaque++;
            report.CategorizeColor(sample.Rgb.ToOklch());
        }

        report.SamplesTotal  = total;
        report.SamplesOpaque = opaque;
        report.OpacityTotal  = opacity;

        return report;
    }

    public static int CalculateIteratorStep(Size size)
    {
        var area = size.Width * size.Height;
        var d = Math.Sqrt(area / 4000.0);
        return d.RoundInt().EvenFloor().Clamp(4, 32);
    }

    public static void CategorizeColor(this ImageScanReport report, Oklch color)
    {
        if (color.C < 0.02 || color.H.IsNaN())
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

    [MethodImpl(AggressiveInlining)]
    public static void PutGray
        (this ImageScanReport report, Oklch color)
    {
        var gix = Math.Floor(color.L * 6).RoundInt().Cap(5);
        report.Grays[gix]++;
    }

    [MethodImpl(AggressiveInlining)]
    public static void PutWeak
        (this ImageScanReport report, Oklch color)
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

    [MethodImpl(AggressiveInlining)]
    public static void Put
        (ref this ImageScan_Hue report, Oklch color)
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
    
    // v2.2

    /// A set of points for a hue used for color sample mapping.
    public readonly struct HueReference()
    {
        public readonly Oklch[] Points = new Oklch[6];

        public Oklch this[int i]
        {
            get => Points[i];
            set => Points[i] = value;
        }

        public Oklch this[Point i]
        {
            get => Points[(int)i];
            set => Points[(int)i] = value;
        }

        public enum Point : byte
        {
            S, P, XD, XL, D, L,
        }
    }

    public static readonly HueReference[] HueReferences = CalculateHueReferencePoints();

    public static HueReference[] CalculateHueReferencePoints()
    {
        const int N_90 = 360 / 4;

        // Get oklch chroma peaks for each 4th hue
        var peaks = new Oklch[N_90];
        for (var  h =  0; h < 360; h++)
        for (byte l = 45; l <  55; l++)
        {
            var oklch = new HSL(h, 100, l).ToRgb24().ToOklch();
            var i = oklch.IntH % 360 / 4;
            if (oklch.C > peaks[i].C)
                peaks[i] = oklch;
        }

        // Calculate a palette for each 4th hue
        var pointSets = new HueReference[N_90];
        for (var i = 0; i < N_90; i++)
        {
            pointSets[i] = new HueReference();

            var peak = peaks[i];
            var top  = peak with { C = peak.C - 0.02 };
            var pale = peak with { C = 0.0.LerpTo(top.C, 0.33), L = 0.5.LerpTo(top.L, 0.33) };
            pointSets[i][HueReference.Point.S] = top;
            pointSets[i][HueReference.Point.P] = pale;

            // m = (y₂ − y₁) / (x₂ − x₁)    slope
            // b = y₁ − mx₁                 y value for x=0
            // y = mx + b
            // x = (y - b) / m
            // y = C, x = L
            var y = 0.05; // C
            {
                var m = (peak.C - 0) / (peak.L - 0);
                var x = y / m;
                var xd = peak with { C = y, L = x + 0.08 };
                var  d = peak with { C = xd.C.HalfwayTo(top.C), L = xd.L.HalfwayTo(top.L) };
                pointSets[i][HueReference.Point.XD] = xd;
                pointSets[i][HueReference.Point. D] =  d;
            }
            {
                var m = (peak.C - 0) / (peak.L - 1);
                var x = (y + m) / m;
                var xl = peak with { C = y, L = x - 0.08 };
                var  l = peak with { C = xl.C.HalfwayTo(top.C), L = xl.L.HalfwayTo(top.L) };
                pointSets[i][HueReference.Point.XL] = xl;
                pointSets[i][HueReference.Point. L] =  l;
            }

            // top      - peak - 0.02 C
            // bottom 2 - triangle sides × C=0.05 +-0.08L
            // center 2 - mid point of top-bottoms
            // pale     - 1/3 from C0 L0.50 to top
        }

        return pointSets;
    }
}
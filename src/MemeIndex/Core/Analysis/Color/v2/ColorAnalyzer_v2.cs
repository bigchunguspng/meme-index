using ColorHelper;
using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color.v2;

/* ===============  Legend  =============== *\

  H | Hue       D | Dark        N | Neutral
  G | Gray      L | Light
                X | Extra

  Bold -       high chroma
  Pale -       low  chroma
  Weak - extra low  chroma (subset of ^)

  Options - colors inside a hue/gray group
    also: o, op, opt, ops

  Primary hues: Red, Orange, Yellow, …
  Vague   hues: Red/Orange, Orange/Yellow, …

\* ======================================== */

/*public class ImageScanReport
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
        MaxC, MinC;#1#

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
}*/

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

public enum HueOption : byte
{
    S, P, D, L, XD, XL,
}

public class ImageScanReport_v22
{
    public int
        SamplesTotal  = 0, // Say 10k
        SamplesOpaque = 0; // < ^

    public long
        OpacityTotal  = 0; // up to 255 * ^^ = 2.5M

    // Values in each bucket: < SamplesTotal

    /// Black, 4 shades of gray, White.
    public readonly double[] Scores_Gray
        = new double[ColorAnalyzer_v2.N_OPS_G];

    /*public int
        WeakHot_D, WeakHot_L,
        WeakCoolD, WeakCoolL;*/

    /// Contains primary hues and their combinations.
    /// Red, R/O, Orange, …, Pink, P/R.
    public readonly ImageScan_Hue_v22[] Hues
        = ColorAnalyzer_v2.B_HUES.Times(_ => new ImageScan_Hue_v22());
}

public readonly struct ImageScan_Hue_v22()
{
    public readonly double[] Scores = new double[ColorAnalyzer_v2.N_OPS_H];

    public double this[int i]
    {
        get => Scores[i];
        set => Scores[i] = value;
    }

    public double this[HueOption i]
    {
        get => Scores[(int)i];
        set => Scores[(int)i] = value;
    }

    public void Combine(ImageScan_Hue_v22 other, double multiplier)
    {
        for (var i = 0; i < ColorAnalyzer_v2.N_OPS_H; i++)
        {
            Scores[i] += multiplier * other.Scores[i];
        }
    }
}

public static class ColorAnalyzer_v2
{
    public const int
        N_OPS_G = 5,
        N_OPS_H = 6,
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
    public static ImageScanReport_v22 ScanImage(Image<Rgba32> image)
    {
        var report = new ImageScanReport_v22();

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

    public static void CategorizeColor(this ImageScanReport_v22 report, Oklch color)
    {
        if (color.C < 0.02 || color.H.IsNaN())
            report.PutGray(color);
        else
        {
            /*if (color.C < 0.05)
                report.PutWeak(color);*/

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
        (this ImageScanReport_v22 report, Oklch color)
    {
        // Get distances to all grayscale L values.
        var distances =
            (Span<(int index, double value)>)
            stackalloc (int, double)[N_OPS_G];
        for (var i = 0; i < N_OPS_G; i++)
        {
            var ref_L = GrayReferences_L[i];
            var distance = Math.Abs(ref_L - color.L);
            if (distance <= 0.02)
            {
                report.Scores_Gray[i] += 50.0; // 1 / 0.02
                return; // Closest point -> return max score.
            }

            distances[i] = (i, distance);
        }

        // Sort asc by distance (smaller distance = bigger score).
        distances.Sort((x1, x2) => (int)(x1.value - x2.value));

        // Take 2 smallest distances, or even less if there is a 2x drop.
        var take = distances[0].value * 2 < distances[1].value ? 1 : 2;
        var smallest_distances = distances.Slice(0, take);

        // Increase score for 1-2 closest points.
        for (var i = 0; i < take; i++)
        {
            var distance = smallest_distances[i].value;
            var opt_ix   = smallest_distances[i].index;
            report.Scores_Gray[opt_ix] += 1 / distance; // distance > 0 due to early return
        }
    }

    // a grave of void PutWeak(this ImageScanReport report, Oklch color)
    // cool = color.H is > 110 and < 315; // otherwise - hot

    [MethodImpl(AggressiveInlining)]
    public static void Put
        (ref this ImageScan_Hue_v22 report, Oklch color)
    {
        // Get distances to all points for given hue.
        var points = HueReferences[color.IntH_safe >> 2].Points;
        var square_distances =
            (Span<(int index, double value)>)
            stackalloc (int, double)[N_OPS_H];
        for (var i = 0; i < N_OPS_H; i++)
        {
            var point = points[i];
            var a  = point.L - color.L;
            var b  = point.C - color.C;
            var cc = a * a + b * b * 9; // stretch C scale 3 times
            if (cc <= 0.0004) // if distance <= 0.02
            {
                report[i] += 50.0; // 1 / sqrt(0.0004)
                return; // Closest point -> return max score.
            }

            square_distances[i] = (i, cc);
        }

        // Sort asc by distance (smaller distance = bigger score).
        square_distances.Sort((x1, x2) => (int)(x1.value - x2.value));

        // Take 3 smallest distances, or even less if there is a 2x drop.
        var take = 3;
        for (var i = 1; i < 3; i++)
        {
            // if next distance² is 4 times bigger (distance is twice as long) - take only what's before
            if (square_distances[i - 1].value * 4 < square_distances[i].value)
            {
                take = i;
            }
        }
        var smallest_square_distances = square_distances.Slice(0, take);

        // Increase score for 1-3 closest points.
        for (var i = 0; i < take; i++)
        {
            var distance = smallest_square_distances[i].value.FastPow(0.5);
            var opt_ix   = smallest_square_distances[i].index;
            report[opt_ix] += 1 / distance; // distance > 0 due to early return
        }
    }

    // HUE REFS, PALETTE

    /// A set of points for a hue used for color sample mapping.
    public readonly struct HueReference()
    {
        public readonly Oklch[] Points = new Oklch[6];

        public Oklch this[int i]
        {
            get => Points[i];
            set => Points[i] = value;
        }

        public Oklch this[HueOption i]
        {
            get => Points[(int)i];
            set => Points[(int)i] = value;
        }
    }

    private static readonly int[] HR_Indices_Palette =
        [ 5, 15, 25, 30,  35, 42, 57, 65,  75, 85 ]; // ROYL GCSB VP

    public static HueReference GetPalette(int hue_index)
    {
        return HueReferences[hue_index];
    }

    public static IEnumerable<HueReference> GetPalette()
    {
        foreach (var hue_index in HR_Indices_Palette)
        {
            yield return HueReferences[hue_index];
        }
    }

    public static readonly       double[] GrayReferences_L = [0.0, 0.25, 0.5, 0.75, 1.0];
    public static readonly HueReference[]  HueReferences = CalculateHueReferencePoints();

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
            pointSets[i][HueOption.S] = top;
            pointSets[i][HueOption.P] = pale;

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
                pointSets[i][HueOption.XD] = xd;
                pointSets[i][HueOption. D] =  d;
            }
            {
                var m = (peak.C - 0) / (peak.L - 1);
                var x = (y + m) / m;
                var xl = peak with { C = y, L = x - 0.08 };
                var  l = peak with { C = xl.C.HalfwayTo(top.C), L = xl.L.HalfwayTo(top.L) };
                pointSets[i][HueOption.XL] = xl;
                pointSets[i][HueOption. L] =  l;
            }

            // top      - peak - 0.02 C
            // bottom 2 - triangle sides × C=0.05 +-0.08L
            // center 2 - mid point of top-bottoms
            // pale     - 1/3 from C0 L0.50 to top
        }

        return pointSets;
    }
}
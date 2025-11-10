using SixLabors.ImageSharp;

namespace MemeIndex_Core.Utils.Geometry;

public class BezierLimitBuilder
{
    private int[]? _limits;

    /// Returns <b>saturation</b> values which separate color groups.
    /// Array indexes represent <b>lightness</b> values.
    /// <param name="points">
    /// An array of 8 points:
    /// <li>First 4 defines Bezier curve for darker part.</li>
    /// <li>Last 4 defines Bezier curve for lighter part.</li>
    /// </param>
    public int[] GetLimits(PointF[] points)
    {
        if (_limits != null) return _limits;

        return _limits = BuildBezier(points);
    }

    private static int[] BuildBezier(PointF[] p)
    {
        var xs = new double[101];

        var b1 = new Bezier([p[0].X, p[1].X, p[2].X, p[3].X], [p[0].Y, p[1].Y, p[2].Y, p[3].Y]);
        var b2 = new Bezier([p[4].X, p[5].X, p[6].X, p[7].X], [p[4].Y, p[5].Y, p[6].Y, p[7].Y]);

        for (var y = 00; y <   50; y++) xs[y] = b1.GetX(b1.GetT_ByY(y));
        for (var y = 50; y <= 100; y++) xs[y] = b2.GetX(b2.GetT_ByY(y));

        return xs.Select(x => x.RoundToInt()).ToArray();
    }
}
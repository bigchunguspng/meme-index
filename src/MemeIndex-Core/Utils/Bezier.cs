namespace MemeIndex_Core.Utils;

/// <param name="xs">X values (EXACTLY 4 items).</param>
/// <param name="ys">Y values (EXACTLY 4 items).</param>
public class Bezier(double[] xs, double[] ys)
{
    public double GetX(double t) => GetX(t, xs);
    public double GetY(double t) => GetX(t, ys);

    private static double GetX(double t, double[] points)
    {
        var tSub = 1 - t;
        return Math.Pow(tSub, 3) * points[0] +
               Math.Pow(tSub, 2) * points[1] * t    * 3 +
               Math.Pow(t,    2) * points[2] * tSub * 3 +
               Math.Pow(t,    3) * points[3];
    }

    public double GetT_ByX(double x, double epsilon = 0.0001) => GetT(x, xs, epsilon);
    public double GetT_ByY(double y, double epsilon = 0.0001) => GetT(y, ys, epsilon);

    private static double GetT(double target, double[] points, double epsilon = 0.0001)
    {
        double t = 0.5, tMin = 0.0, tMax = 1.0;

        while (tMax - tMin > epsilon)
        {
            var x = GetX(t, points);

            var closeEnough = Math.Abs(x - target) < epsilon;
            if (closeEnough) return t;

            if (x < target) tMin = t;
            else /*      */ tMax = t;

            t = (tMin + tMax) / 2.0;
        }

        return t;
    }
}
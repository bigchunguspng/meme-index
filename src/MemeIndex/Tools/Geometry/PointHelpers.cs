using SixLabors.ImageSharp;

namespace MemeIndex.Tools.Geometry;

public static class PointHelpers
{
    public static bool IsInside(this Point point, Rectangle rectangle)
    {
        return point.X >= rectangle.Left
            && point.X <= rectangle.Right
            && point.Y >= rectangle.Top
            && point.Y <= rectangle.Bottom;
    }

    public static double GetDistanceToRectangleCenter(this Point point, Rectangle rectangle)
    {
        var rectangleCenter = rectangle.Location + rectangle.Size / 2;
        var a = Math.Abs(point.X - rectangleCenter.X);
        var b = Math.Abs(point.Y - rectangleCenter.Y);
        return Math.Sqrt(a * a + b * b);
    }
}
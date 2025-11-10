using System.Collections;
using SixLabors.ImageSharp;

namespace MemeIndex_Core.Utils.Geometry;

/// Iterates a <see cref="Size"/> object via 45° dot grid.
/// <param name="size">Object for iterating.</param>
/// <param name="step">Vertical and horizontal distance between dots on the grid.</param>
public class SizeIterator(Size size, int step) : IEnumerable<Point>
{
    public IEnumerator<Point> GetEnumerator()
    {
        var halfStep = step / 2;
        var row = 0;
        for (var y = 0; y < size.Height; y += halfStep)
        {
            var oddRow = row % 2 != 0;

            for (var x = oddRow ? halfStep : 0; x < size.Width; x += step)
            {
                yield return new Point(x, y);
            }

            row++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
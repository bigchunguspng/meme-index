namespace MemeIndex.Tools.Geometry;

internal readonly struct BytePoint(byte x, byte y)
{
    public byte X { get; } = x;
    public byte Y { get; } = y;

    public override bool Equals(object? obj)
    {
        return obj is BytePoint other
            && X.Equals(other.X)
            && Y.Equals(other.Y);
    }

    public override int GetHashCode()
    {
        return Y << 8 ^ X;
    }
}
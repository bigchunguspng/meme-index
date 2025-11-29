namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Math
{
    // F -> I
    [MethodImpl(AggressiveInlining)] public static int   RoundInt(this double x) => (int)Math.Round  (x);
    [MethodImpl(AggressiveInlining)] public static int   RoundInt(this float  x) => (int)Math.Round  (x);
    [MethodImpl(AggressiveInlining)] public static int CeilingInt(this double x) => (int)Math.Ceiling(x);
    [MethodImpl(AggressiveInlining)] public static int CeilingInt(this float  x) => (int)Math.Ceiling(x);

    // CLAMP
    [MethodImpl(AggressiveInlining)] public static   int Clamp(this int  x, int  min, int  max) => Math.Clamp(x, min, max);
    [MethodImpl(AggressiveInlining)] public static   int Cap  (this int  x,           int  max) => Math.Min  (x,      max);
    [MethodImpl(AggressiveInlining)] public static  byte Cap  (this byte x,           byte max) => Math.Min  (x,      max);

    [MethodImpl(AggressiveInlining)] public static  byte ClampByte (this int x) =>  (byte)Math.Clamp(x,    0, 255);
    [MethodImpl(AggressiveInlining)] public static sbyte ClampSbyte(this int x) => (sbyte)Math.Clamp(x, -128, 127);

    // CHECK
    [MethodImpl(AggressiveInlining)] public static bool IsNaN(this double value) => double.IsNaN(value);

    [MethodImpl(AggressiveInlining)] public static bool IsOdd (this int x) => (x & 1) == 1;
    [MethodImpl(AggressiveInlining)] public static bool IsEven(this int x) => (x & 1) == 0;

    // TO EVEN
    [MethodImpl(AggressiveInlining)] public static int ToEven     (this int x) => x & ~1;
    [MethodImpl(AggressiveInlining)] public static int   EvenFloor(this int x) => x >> 1 << 1;

    // GAP
    [MethodImpl(AggressiveInlining)] public static float Gap(this int   outer, int   inner) => (outer - inner) / 2F;
    [MethodImpl(AggressiveInlining)] public static float Gap(this float outer, float inner) => (outer - inner) / 2F;

    //
    /// Up to 33% faster than <see cref="Math.Pow"/>. Result can differ by N,e-16 - round if necessary!
    [MethodImpl(AggressiveInlining)] public static double FastPow(this double x, double y) => Math.Exp(y * Math.Log(x));
}
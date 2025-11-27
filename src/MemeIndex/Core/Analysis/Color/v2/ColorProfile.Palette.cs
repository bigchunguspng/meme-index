using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color.v2;

public static partial class ColorProfile
{
    /*
palette generator:
for each rgb - turn to oklch
divide into N buckets. N = 11 hues * 3 shades * 3 chromas = 99
for each bucket
- get number of colors, calculate score multiplier (more samples -> lower value, saturated give more impact)
- select average color in bucket for palette (saturated give more impact)
- return button grid hue > 4-6 subdivs [some are empty]
     */

    private struct ColorBucket_Oklch
    {
        public double TotalL, TotalC, TotalH;
        public int Samples;

        public Oklch GetAverage()
        {
            return new Oklch(TotalL / Samples, TotalC / Samples, TotalH / Samples + OFF);
        }
    }

    public static void GeneratePalette_Saturated()
    {
        var sw = Stopwatch.StartNew();
        var buckets = new ColorBucket_Oklch[N_of_HUES * N_of_Cs * N_of_Ls];
        for (var r = 0; r < 256; r++)
        for (var g = 0; g < 256; g++)
        for (var b = 0; b < 256; b++)
        {
            var ok = new Rgb24((byte)r, (byte)g, (byte)b).ToOklch();
            if (ok.C < CHROMA_GRAY) continue;

            var bi = GetBucketIndex(ok);
            var bucket = buckets[bi];
            bucket.TotalL += ok.L;
            bucket.TotalC += ok.C;
            bucket.TotalH += (ok.H - OFF + 360) % 360;
            bucket.Samples++;
            buckets[bi] = bucket;
        }

        using var image = new Image<Rgb24>(N_of_HUES, N_of_Cs * N_of_Ls);
        for (var y = 0; y < N_of_Cs * N_of_Ls; y++) // 0..5
        for (var x = 0; x < N_of_HUES; x++) // 0..10
        {
            // red = 0..5
            var bi = y + x * N_of_Cs * N_of_Ls;
            image[x, y] = buckets[bi].GetAverage().ToRgb24();
        }
        image.SaveAsPng(Dir_Debug_Color.EnsureDirectoryExist().Combine("palette.png"));
        sw.Log("Palette generated");
    }

    public static void RenderHues()
    {
        var image = new Image<Rgb24>(101 * 4, 101 * 3, 40.ToRgb24());
        var his = new double[N_of_HUES];
        for (var hi = 0; hi < N_of_HUES; hi++)
        {
            var a = hi == 0 ? 0 : _borders_H[hi - 1];
            var b = _borders_H[hi];
            var h = (a + b) / 2.0 + OFF;
            his[hi] = h;
        }
        for (var r = 0; r < 256; r++)
        for (var g = 0; g < 256; g++)
        for (var b = 0; b < 256; b++)
        {
            var ok = new Rgb24((byte)r, (byte)g, (byte)b).ToOklch();
            var hi = GetHueIndex(ok.H);
            if (Math.Abs(ok.H - his[hi]) > 2.0) continue;

            var row = hi / 4;
            var col = hi % 4;
            var l = (ok.L * 100).RoundInt().Clamp(0, 100);
            var c = (ok.C * 300).RoundInt().Clamp(0, 100);
            image[col * 101 + l, row * 101 + 100 - c] = ok.ToRgb24();
        }
        var path = Dir_Debug_Color.EnsureDirectoryExist().Combine($"Hues-{Desert.GetSand()}.png");
        image.SaveAsPng(path);
    }
}
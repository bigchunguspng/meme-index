using ColorHelper;
using MemeIndex.Tools.Geometry;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex.Core.Analysis.Color;

public static class DebugTools
{
    private const int SIDE = 101;

    public static readonly JpegEncoder JpegEncoder_Q80 = new() { Quality = 80 };

    public static void Test()
    {
        using var report_1 = new Image<Rgb24>(360, 101, new Rgb24(50, 50, 50));
        using var report_2 = new Image<Rgb24>(360, 101, new Rgb24(50, 50, 50));

        var color = 40.ToRgb24(); 
        for (var r_hi = 0; r_hi < 12; r_hi += 2)
        {
            var rect = new RectangleF(r_hi * 30, 0, 30, 360);
            report_1.Mutate(x => x.Fill(color, rect));
            report_2.Mutate(x => x.Fill(color, rect));
        }

        for (byte s = 0; s < 100; s++)
        for (var  h = 0; h < 360; h++)
        for (byte l = 0; l < 100; l++)
        {
            var hsl = new HSL(h, s, l);
            var rgb = ColorConverter.HslToRgb(hsl).ToRgb24();
            var HSL = ColorConverter.RgbToHsl(rgb.ToRGB());
            var OkLCH = rgb.ToOklch();
            //if (Math.Abs(hsl.L - HSL.L) > 5) LogError($"{hsl.H},{hsl.S},{hsl.L} != {HSL.H},{HSL.S},{HSL.L}");
            report_1[HSL.H.Clamp(0, 360 - 1), HSL.L] = rgb;
            report_2[OkLCH.H.RoundInt().Clamp(0, 360 - 1), (OkLCH.L * 100).RoundInt().Clamp(0, 100)] = rgb;
        }
            
        var name_1 = $"Test-{DateTime.UtcNow.Ticks}-H-Full.png";
        var name_2 = $"Test-{DateTime.UtcNow.Ticks}-O-Full.png";
        var save_1 = Dir_Debug_Oklch
            .EnsureDirectoryExist()
            .Combine(name_1);
        var save_2 = Dir_Debug_Oklch
            .Combine(name_2);

        report_1.SaveAsPng(save_1);
        report_2.SaveAsPng(save_2);
    }

    public static void LoopHSL(int step)
    {
        for (var hue = 0; hue < 360; hue += step) HSL(hue);
    }

    public static void LoopOklch(double step = 0.05)
    {
        for (var chroma = 0.0; chroma < 1; chroma += step) Oklch_HxL(chroma);
    }

    public static void Oklch_HxL(double chroma)
    {
        using var image = new Image<Rgb24>(360, 100);
        for (var l = 0; l < image.Height; l++)
        for (var h = 0; h < image.Width; h++)
        {
            var hsl = new HSL(h, (byte)(chroma * 100).RoundInt(), (byte)l);
            var rgb = ColorConverter.HslToRgb(hsl).ToRgb24();
            //var oklch = new Oklch(l / 100.0, chroma, h);
            var oklch = rgb.ToOklch();
            var x =  oklch.H       .RoundInt().Clamp(0, 360 - 1);
            var y = (oklch.L * 100).RoundInt().Clamp(0, 100 - 1);
            var color = oklch.ToRgb24();
            image[x, y] = color;
        }
        var path = Dir_Debug_Oklch
            .EnsureDirectoryExist()
            .Combine($"{nameof(Oklch_HxL)}-HxL-2-{chroma:F2}.png");
        image.SaveAsPng(path);
    }

    public static void Oklch(int hue)
    {
        var sw = Stopwatch.StartNew();

        using var image = new Image<Rgb24>(100, 100);

        for (var x = 0; x < image.Width; x++)
        for (var y = 0; y < image.Height; y++)
        {
            var oklch = new Oklch(x / 100.0, y / 100.0, hue);
            var color = oklch.ToRgb24();
            image[x, y] = color;
        }

        sw.Log("hue filled");

        var path = Dir_Debug_Oklch
            .EnsureDirectoryExist()
            .Combine($"{nameof(Oklch)}-{hue}.png");
        image.SaveAsPng(path);
        sw.Log("image rendered");
    }

    public static void HSL(int hue)
    {
        var sw = Stopwatch.StartNew();

        using var image = new Image<Rgb24>(100, 100);

        for (var x = 0; x < image.Width; x++)
        for (var y = 0; y < image.Height; y++)
        {
            var hsl = new HSL(hue, (byte)x, (byte)y);
            var rgb = ColorConverter.HslToRgb(hsl);
            var color = new Rgb24(rgb.R, rgb.G, rgb.B);
            image[x, y] = color;
        }

        sw.Log("hue filled");

        var path = Dir_Debug_HSL
            .EnsureDirectoryExist()
            .Combine($"{nameof(HSL)}-{hue}.png");
        image.SaveAsPng(path);
        sw.Log("image rendered");
    }

    public static void RenderSamplePoster(FilePath path)
    {
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("image is loaded");

        var step = ColorTagService.CalculateStep(source.Size);
        var halfStep = step / 2;
        Log($"using step - {step}");

        var w = source.Width;
        var h = source.Height;
        var image_actual = new Image<Rgb24>(w, h);
        var image_poster = new Image<Rgb24>(w, h);

        string[] keys = ["SL", "S1", "S2", "SD", "PD", "PL"];

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var l = hsl.L;
            var s = hsl.S;

            var hue_index = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues

            var key = (char)('A' + hue_index);
            var posterized = s < 5
                ? l < 20 ? 0.ToRgb24() : l > 80 ? 255.ToRgb24() : 128.ToRgb24()
                : ColorSearchProfile.ColorsFunny[key][$"{key}{keys[s > 50 ? l > 50 ? 1 : 2 : l > 50 ? 5 : 4]}"];

            for (var y0 = y - halfStep; y0 < y + halfStep; y0++)
            for (var x0 = x - halfStep; x0 < x + halfStep; x0++)
            {
                if (x0 < 0 || y0 < 0 || x0 >= w || y0 >= h)
                    continue;

                var xd = Math.Abs(x - x0);
                var yd = Math.Abs(y - y0);
                if (xd + yd > halfStep)
                    continue;

                image_actual[x0, y0] = sample;
                image_poster[x0, y0] = posterized;
            }
        }

        var ticks = DateTime.UtcNow.Ticks;
        var sand = Desert.GetSand();
        var jpeg1 = Dir_Debug_SampleGrids
            .EnsureDirectoryExist()
            .Combine($"Samples-{ticks}-{sand}.jpg");
        var jpeg2 = Dir_Debug_SampleGrids
            .EnsureDirectoryExist()
            .Combine($"Poster-{ticks}-{sand}.jpg");
        image_actual.SaveAsJpeg(jpeg1, JpegEncoder_Q80);
        image_poster.SaveAsJpeg(jpeg2, JpegEncoder_Q80);
    }

    public static void RenderProfile_Oklch_HxL(FilePath path)
    {
        var i = 0;
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("1. Load image.");

        using var report = new Image<Rgb24>(360, 101, new Rgb24(50, 50, 50));

        var color = 40.ToRgb24(); 
        for (var r_hi = 0; r_hi < 12; r_hi += 2)
        {
            var rect = new RectangleF(r_hi * 30, 0, 30, 360);
            report.Mutate(x => x.Fill(color, rect));
        }
        sw.Log("2. Draw report background.");

        var step = ColorTagService.CalculateStep(source.Size);
        Log($"[step = {step}]");

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            var oklch = sample.ToOklch();
            var ly = (oklch.L * 100).RoundInt().Clamp(0, 100);
            var hx = (oklch.H.RoundInt() % 360).Clamp(0, 360);

            report[hx, ly] = sample;
            /*if (i++ % 256 == 0 && double.IsNaN(oklch.H).Janai())
            {
                var hue_ix = (oklch.H.RoundInt() + 15) % 360 / 30; // 0..11 => 12 hues
                Console.WriteLine($"[{i,3}] Hue: {oklch.H:F2} -> {hue_ix}");
            }*/
        }
        sw.Log("3. Collect samples.");

        var name = $"Profile-{DateTime.UtcNow.Ticks}-{Desert.GetSand()}-Oklch-HxL.png";
        var save = Dir_Debug_HSL_Profiles
            .EnsureDirectoryExist()
            .Combine(name);
        report.SaveAsPng(save);
        sw.Log($"4. Save report >> \"{name}\"");
    }

    public static void RenderProfile_Oklch(FilePath path)
    {
        var i = 0;
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("1. Load image.");

        using var report = new Image<Rgb24>(SIDE * 3, SIDE * 2, new Rgb24(50, 50, 50));

        for (var row = 0; row < 2; row++)
        for (var col = 0; col < 3; col++)
        {
            report.PutLines(col * SIDE, row * SIDE);
        }
        sw.Log("2. Draw report background.");

        var step = ColorTagService.CalculateStep(source.Size);
        Log($"[step = {step}]");

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            var oklch = sample.ToOklch();
            var ly = (oklch.L * 100  ).RoundInt().Clamp(0, 100);
            var cx = (oklch.C * 212.7).RoundInt().Clamp(0, 100); // max .c = 0.47

            var hue_ix = (oklch.H.RoundInt() + 15) % 360 / 30; // 0..11 => 12 hues
            var offsetX = hue_ix / 4 * SIDE;
            var offsetY = hue_ix % 2 == 0 ? 0 : SIDE;

            report[offsetX + cx, offsetY + ly] = sample;
            /*if (i++ % 256 == 0 && double.IsNaN(oklch.H).Janai())
            {
                Console.WriteLine($"[{i,3}] Hue: {oklch.H:F2} -> {hue_ix}");
            }*/
        }
        sw.Log("3. Collect samples.");

        var name = $"Profile-{DateTime.UtcNow.Ticks}-{Desert.GetSand()}-Oklch.png";
        var save = Dir_Debug_HSL_Profiles
            .EnsureDirectoryExist()
            .Combine(name);
        report.SaveAsPng(save);
        sw.Log($"4. Save report >> \"{name}\"");
    }

    public static void RenderProfile_HSL(FilePath path)
    {
        var i = 0;
        var sw = Stopwatch.StartNew();

        using var source = Image.Load<Rgb24>(path);
        sw.Log("1. Load image.");

        using var report = new Image<Rgb24>(SIDE * 3, SIDE * 2, new Rgb24(50, 50, 50));

        for (var row = 0; row < 2; row++)
        for (var col = 0; col < 3; col++)
        {
            report.PutLines(col * SIDE, row * SIDE);
        }
        sw.Log("2. Draw report background.");

        var step = ColorTagService.CalculateStep(source.Size);
        Log($"[step = {step}]");

        foreach (var (x, y) in new SizeIterator_45deg(source.Size, step))
        {
            var sample = source[x, y];

            var hsl = ColorConverter.RgbToHsl(sample.ToRGB());
            var l = hsl.L;
            var s = hsl.S;

            var hue_ix = (hsl.H + 15) % 360 / 30; // 0..11 => 12 hues
            var offsetX = hue_ix / 4 * SIDE;
            var offsetY = hue_ix % 2 == 0 ? 0 : SIDE;

            report[offsetX + s, offsetY + l] = sample;
            /*if (i++ % 256 == 0 && hsl.H != 0)
            {
                Console.WriteLine($"[{i,3}] Hue: {hsl.H:F2} -> {hue_ix}");
            }*/
        }
        sw.Log("3. Collect samples.");

        var name = $"Profile-{DateTime.UtcNow.Ticks}-{Desert.GetSand()}-HSL.png";
        var save = Dir_Debug_HSL_Profiles
            .EnsureDirectoryExist()
            .Combine(name);
        report.SaveAsPng(save);
        sw.Log($"4. Save report >> \"{name}\"");
    }

    private static void PutLines(this Image<Rgb24> image, int offsetX = 0, int offsetY = 0)
    {
        Rgb24 c1A = 98.ToRgb24(), c2A = 90.ToRgb24(), c3A = 80.ToRgb24(), c4A = 70.ToRgb24(); 
        Rgb24 c1B = 32.ToRgb24(), c2B = 40.ToRgb24(), c3B = 50.ToRgb24(), c4B = 60.ToRgb24(); 

        image.Mutate(x => x.Fill(c2A, new RectangleF(offsetX +  0, offsetY +  0, SIDE, 50))); // Y-DARK
        image.Mutate(x => x.Fill(c2B, new RectangleF(offsetX +  0, offsetY + 50, SIDE, 51))); // Y-LIGHT
        image.Mutate(x => x.Fill(c1A, new RectangleF(offsetX +  0, offsetY +  0, SIDE,  4))); // BLACK
        image.Mutate(x => x.Fill(c1B, new RectangleF(offsetX +  0, offsetY + 97, SIDE,  4))); // WHITE

        var b1 = ColorTagService.GetGrayscaleLimits();
        var b2 = ColorTagService.GetSubPaleLimits();
        var b3 = ColorTagService.GetPaleLimits();
        var b4 = ColorTagService.GetPeakLimits();
        for (var y = 4; y <= 96; y++)
        {
            var c1 = y < 50 ? c4A : c4B; // pale-weak
            var c2 = y < 50 ? c3A : c3B; // pale-strong
            var c3 = y < 50 ? c4B : c4A; // vivid

            var w1 = b1[y];
            var w2 = b2[y];
            var w3 = b3[y];
            var w4 = b4[y];
            var w5 = Math.Max(w1, w4);
            var w6 = Math.Max(w2, w4);

            DrawLine(y, c1, w1,   w2 - w1);
            DrawLine(y, c2, w2,   w3 - w2);
            DrawLine(y, c3, w3, SIDE - w3);
            DrawLine(y, c2, w5,   w3 - w5);
            DrawLine(y, c1, w6,   w3 - w6);
        }

        return;

        void DrawLine(int y, SixLabors.ImageSharp.Color color, int offset, int width)
        {
            image.Mutate(x => x.Fill(color, new RectangleF(offsetX + offset, offsetY + y, width, 1)));
        }
    }
}
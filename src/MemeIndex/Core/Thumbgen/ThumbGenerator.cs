using MemeIndex.Core.Indexing;
using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MemeIndex.Core.Thumbgen;

public static class ThumbGenerator
{
    private static readonly Size _fitSize = new (275, 180); // Similar to Google Images thumb size.

    public static async Task GenerateThumbnails()
    {
        Log("GenerateThumbnails", "START");

        await using var con = await AppDB.ConnectTo_Main();
        var files = await con.Files_GetToBeThumbed();
        await con.CloseAsync();
        Log("GenerateThumbnails", "GET FILES TBT");

        foreach (var file in files)
        {
            try
            {
                var sw2 = Stopwatch.StartNew();
                var result = await GenerateThumbnail(file.GetPath(), file.id);
                Log($"Thumbed file {file.id,5}");
                FileProcessor.time_tg += sw2.GetElapsed_Restart();

                await FileProcessor.C_ThumbgenSave.Writer.WriteAsync(result);
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }

        Log("GenerateThumbnails", "DONE");
    }

    private static readonly WebpEncoder _encoder = new()
    {
        Quality = 85,
        Method = WebpEncodingMethod.BestQuality,
        TransparentColorMode = WebpTransparentColorMode.Clear,
    };

    public static async Task<ThumbgenResult> GenerateThumbnail(string path, int file_id)
    {
        var sw = Stopwatch.StartNew();
        using var image = await Image.LoadAsync(path);
        t1 += sw.GetElapsed_Restart();
        var sizeOG = image.Size;
        var sizeTN = image.Size.FitSize(_fitSize);
        image.Mutate(ctx => ctx.Resize(sizeTN, CubicResampler.RobidouxSharp, compand: false));
        t2 += sw.GetElapsed_Restart();
        var save = Dir_Thumbs
            .EnsureDirectoryExist()
            .Combine($"{file_id:x6}.webp");
        await image.SaveAsWebpAsync(save, _encoder);
        t3 += sw.GetElapsed_Restart();

        return new ThumbgenResult(file_id, DateTime.UtcNow, sizeOG);
    }

    public static TimeSpan
        t1 = TimeSpan.Zero,
        t2 = TimeSpan.Zero,
        t3 = TimeSpan.Zero;

    public static void PrintStats()
    {
        Log($"""
             THUMBGEN DONE:
                 LOAD: {t1.ReadableTime(),10} | {(t1 / FileProcessor.N_files).ReadableTime(),10} per file
                 SIZE: {t2.ReadableTime(),10} | {(t2 / FileProcessor.N_files).ReadableTime(),10} per file
                 SAVE: {t3.ReadableTime(),10} | {(t3 / FileProcessor.N_files).ReadableTime(),10} per file
             """);
    }

    // TEST

    private static readonly IResampler[] samplers =
    [
        new BicubicResampler(),
        new BoxResampler(),
        CubicResampler.CatmullRom,
        CubicResampler.Hermite,
        CubicResampler.MitchellNetravali,
        CubicResampler.Robidoux,
        CubicResampler.RobidouxSharp,
        //CubicResampler.Spline,
        LanczosResampler.Lanczos2,
        LanczosResampler.Lanczos3, // N
        LanczosResampler.Lanczos5,
        LanczosResampler.Lanczos8,
        new TriangleResampler(),
        new WelchResampler(),
        //new NearestNeighborResampler(),
    ];

    private static readonly string[] names =
    [
        "bic",
        "box",
        "cCR",
        "cH",
        "cMN",
        "cRo",
        "cRS",
        //"cS",
        "lz2",
        "lz3",
        "lz5",
        "lz8",
        "tri",
        "wel",
        //"nea",
    ];

    private static readonly bool[] compands = [true, false];

    public static void Test(string path)
    {
        for (var si = 0; si < samplers.Length; si++)
        for (var ci = 0; ci < compands.Length; ci++)
        {
            var sw = Stopwatch.StartNew();
            var sampler = samplers[si];
            var compand = compands[ci];
            var suffix = names[si];
            var comp = compand ? "Y" : "N";
            var save = Dir_Thumbs
                .EnsureDirectoryExist()
                .Combine($"{suffix}-{comp}.webp");
            for (var i = 0; i < 20; i++)
            {
                var image = Image.Load(path);
                var size = image.Size.FitSize(_fitSize);
                image.Mutate(ctx => ctx.Resize(size, sampler, compand));
                image.SaveAsWebp(save);
            }

            var el = sw.Elapsed;
            var fileSize = new FileInfo(save).Length;
            // time, size, score
            Log($"{(el / 10).ReadableTime(),-10} | {fileSize,5} |= {10_000.0 / (fileSize * el.TotalSeconds):F3} | {suffix} / {comp}");
        }
    }
}
using System.Threading.Channels;
using MemeIndex.DB;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private static readonly Size _fitSize = new (275, 180); // Similar to Google Images thumb size.

    public readonly Channel<ThumbgenContext>
        //C_Resize   = Channel.CreateUnbounded<ThumbgenContext>(),
        C_SaveWebp = Channel.CreateUnbounded<ThumbgenContext>();

    private async Task GenerateThumbnails()
    {
        const string code = "GenerateThumbnails";
        Log(code, "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var db_files = await con.Files_GetToBeThumbed();
        await con.CloseAsync();
        Log(code, "GET FILES");

        var files = db_files.Select(x => x.Compile()).ToArray();
        if (files.Length == 0)
        {
            Log(code, "NOTHING TO PROCESS");
            return;
        }

        ImagePool.Book(files.Select(x => x.Path), files.Length);

        if (job_DB.ExecuteTask == null)
            await job_DB.StartAsync(CancellationToken.None);

        await job_thumbsWebp.StartAsync(CancellationToken.None);

        foreach (var file in files)
        {
            try
            {
                var ctx = new ThumbgenContext(file);
                //await Thumbnail_Load(ctx);
                await Thumbnail_Resize(ctx);
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }

        C_SaveWebp.Writer.Complete();

        Log(code, "DONE");
    }

    private static readonly WebpEncoder _encoder = new()
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 85,
        Method = WebpEncodingMethod.Level2,
        TransparentColorMode = WebpTransparentColorMode.Clear,
    };

    /*public async Task Thumbnail_Load(ThumbgenContext ctx)
    {
        Logger.LogStart(THUMB_LOAD, ctx.FileId);
        ctx.Source = await ImagePool.Load(ctx.Path);
        Logger.LogEnd  (THUMB_LOAD, ctx.FileId);
        await C_Resize.Writer.WriteAsync(ctx);
    }*/

    // todo - thumb load logic -> thumb resize
    
    public async Task Thumbnail_Resize(ThumbgenContext ctx)
    {
        Tracer.LogOpen(ctx.FileId, THUMB_LOAD);
        ctx.Source = await ImagePool.Load(ctx.Path);
        Tracer.LogJoin(ctx.FileId, THUMB_LOAD, THUMB_SIZE);
        var size = ctx.Source.Size.FitSize(_fitSize);
        ctx.Thumb = ctx.Source.Clone(x => x.Resize(size, LanczosResampler.Lanczos3, compand: false));
        ImagePool.Return(ctx.Path);
        Tracer.LogDone(ctx.FileId, THUMB_SIZE);
        await C_SaveWebp.Writer.WriteAsync(ctx);
    }

    public async Task Thumbnail_Save(ThumbgenContext ctx)
    {
        Tracer.LogOpen(ctx.FileId, THUMB_SAVE);
        var save = Dir_Thumbs
            .EnsureDirectoryExist()
            .Combine($"{ctx.FileId:x6}.webp");
        await ctx.Thumb.SaveAsWebpAsync(save, _encoder);
        Tracer.LogDone(ctx.FileId, THUMB_SAVE);
        LogDebug($"File {ctx.FileId,6} -> thumbnail generated");

        var result = ctx.ToDB_File();
        await C_DB_Write.Writer.WriteAsync(async connection =>
        {
            Tracer.LogOpen(ctx.FileId, DB_W_FT);
            await connection.File_UpdateDateThumbGenerated(result);
            Tracer.LogDone(ctx.FileId, DB_W_FT);
        });
    }
}

public struct ThumbgenContext(FilePathRecord file)
{
    public readonly string Path   = file.Path;
    public readonly int    FileId = file.Id;
    public Image  Source;
    public Image  Thumb;

    public DB_File_UpdateDateSize ToDB_File
        () => new(FileId, DateTime.UtcNow, Source.Size);
}
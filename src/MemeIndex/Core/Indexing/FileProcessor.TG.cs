using System.Threading.Tasks.Dataflow;
using MemeIndex.DB;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private async Task TG_Start(StatusScope status)
    {
        const string CODE = "FP-TG";
        Log(CODE, "START");

        var db_files = await _con.Files_GetToBeThumbed();
        if (db_files.Count == 0)
        {
            Log(CODE, "NOTHING TO PROCESS");
            return;
        }
        Log(CODE, "GET FILES");

        App.SetStatus(status.Status);

        var files = db_files.Select(x => new IX_Ctx
        {
            Id = x.id,
            Path = x.GetPath(),
        }).ToArray();
        Log(CODE, "GET FILES*");

        var b_load    = new TransformBlock<IX_Ctx, IX_Ctx> (FP_Load,   exec_ops_4cores_16files);
        var b_resize  = new TransformBlock<IX_Ctx, TG_Ctx> (TG_Resize, exec_ops_4cores_16files);
        var b_webp    = new TransformBlock<TG_Ctx, DB_Task>(TG_Save,   exec_ops_4cores_16files);
        b_load   .LinkTo( b_resize,  link_ops_propagateCompletion);
        b_resize .LinkTo( b_webp,    link_ops_propagateCompletion);
        b_webp   .LinkTo(_b_batch);
        Log(CODE, "DEFINE BLOCKS");

        ImagePool.Book(files.Select(x => x.Path), files.Length);

        foreach (var file in files)
        {
            if ((await b_load.SendAsync(file)).Failed())
                LogWarn(CODE, $"FAILED TO SEND FILE {file.Id}");
        }
        Log(CODE, "SEND FILES");

        b_load.Complete();
        await b_webp.Completion;
        
        Log(CODE, "DONE");
    }

    private TG_Ctx TG_Resize(IX_Ctx ix_ctx)
    {
        var ctx = new TG_Ctx(ix_ctx);
        Tracer.LogOpen(ctx.Id, TG_SIZE);
        var size  = ctx.Image.Size.FitSize(_fitSize);
        var thumb = ctx.Image.Clone(x => x.Resize(size, LanczosResampler.Lanczos3, compand: false));
        ImagePool.Return(ctx.Path);
        ctx.Thumb = thumb;
        ctx.OG_Size = ctx.Image.Size;
        ctx.Image = null!;
        Tracer.LogDone(ctx.Id, TG_SIZE);
        return ctx;
    }

    private async Task<DB_Task> TG_Save(TG_Ctx ctx)
    {
        var tdate = DateTime.UtcNow;
        Tracer.LogOpen(ctx.Id, TG_SAVE);
        var save = Dir_Thumbs
            .EnsureDirectoryExist()
            .Combine(GetThumbFilename(ctx.Id, tdate));
        await ctx.Thumb.SaveAsWebpAsync(save, _encoder);
        ctx.DB_File = new DB_File_UpdateDateSize(ctx.Id, tdate, ctx.OG_Size);
        Tracer.LogDone(ctx.Id, TG_SAVE);
        return (con, tx) => IX_ThumbnailWriteDB(ctx, con, tx);
    }

    private async Task IX_ThumbnailWriteDB
        (TG_Ctx ctx, SqliteConnection con, SqliteTransaction tx)
    {
        Tracer.LogOpen(ctx.Id, DB_W_FT);
        await con.File_UpdateDateThumbGenerated(ctx.DB_File, tx);
        Tracer.LogDone(ctx.Id, DB_W_FT);
    }

    //

    private static readonly Size _fitSize = new (174, 174);

    private static readonly WebpEncoder _encoder = new()
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 85,
        Method = WebpEncodingMethod.Level2,
        TransparentColorMode = TransparentColorMode.Clear,
    };

    [MethodImpl(AggressiveInlining)]
    public static string GetThumbFilename
        (int id, DateTime tdate) => $"{id}-{new DateTimeOffset(tdate).ToUnixTimeSeconds()}.webp";
}
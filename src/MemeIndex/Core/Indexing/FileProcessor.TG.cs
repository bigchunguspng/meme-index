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
        Log(NAME_TG, "START");

        var db_files = await _con.Files_GetToBeThumbed();
        if (db_files.Count == 0)
        {
            Log(NAME_TG, "NOTHING TO PROCESS");
            return;
        }
        Log(NAME_TG, "GET FILES");

        App.SetStatus(status.Status);

        var files = db_files.Select(x => new IX_Ctx
        {
            Id = x.id,
            Path = x.GetPath(),
        }).ToArray();
        Log(NAME_TG, "GET FILES*");

        var b_load    = new TransformBlock<IX_Ctx, IX_Ctx> (FP_Load,   exec_ops_4cores_16files);
        var b_resize  = new TransformBlock<IX_Ctx, TG_Ctx> (TG_Resize, exec_ops_4cores_16files);
        var b_webp    = new TransformBlock<TG_Ctx, DB_Task>(TG_Save,   exec_ops_4cores_16files);
        b_load   .LinkTo( b_resize,  link_ops_propagateCompletion);
        b_resize .LinkTo( b_webp,    link_ops_propagateCompletion);
        b_webp   .LinkTo(_b_batch);
        Log(NAME_TG, "DEFINE BLOCKS");

        ImagePool.Book(files.Select(x => x.Path), files.Length);

        foreach (var file in files)
        {
            if ((await b_load.SendAsync(file)).Failed())
                LogError(NAME_TG, $"FAILED TO SEND FILE {file.Id}");
        }
        Log(NAME_TG, "SEND FILES");

        b_load.Complete();
        await b_webp.Completion;
        
        Log(NAME_TG, "DONE");
    }

    private TG_Ctx TG_Resize(IX_Ctx ix_ctx)
    {
        var ctx = new TG_Ctx(ix_ctx);
        if (ctx.Failed)
            return ctx;

        try
        {
            Tracer.LogOpen(ctx.Id, TG_SIZE);
            var size = ctx.Image.Size.FitSize(_fitSize);
            var thumb = ctx.Image.Clone(x => x.Resize(size, LanczosResampler.Lanczos3, compand: false));
            ImagePool.Return(ctx.Path);
            ctx.Thumb = thumb;
            ctx.OG_Size = ctx.Image.Size;
            ctx.Image = null!;
        }
        catch (Exception e)
        {
            App.LogException_JOB(e, NAME_TG);
            ctx.Failed = true;
        }
        finally
        {
            Tracer.LogDone(ctx.Id, TG_SIZE);
        }
        return ctx;
    }

    private async Task<DB_Task> TG_Save(TG_Ctx ctx)
    {
        if (ctx.Failed)
            return IX_MarkBrokenFile(ctx.Id, NAME_TG);

        try
        {
            Tracer.LogOpen(ctx.Id, TG_SAVE);
            var date = DateTime.UtcNow;
            var save = Dir_Thumbs
                .EnsureDirectoryExist()
                .Combine(GetThumbFilename(ctx.Id, date));
            await ctx.Thumb.SaveAsWebpAsync(save, _encoder);
            ctx.DB_File = new DB_File_UpdateDateSize(ctx.Id, date, ctx.OG_Size);
            return (con, tx) => DB_SaveTG(ctx, con, tx);
        }
        catch (Exception e)
        {
            App.LogException_JOB(e, NAME_TG);
            return IX_MarkBrokenFile(ctx.Id, NAME_TG);
        }
        finally
        {
            Tracer.LogDone(ctx.Id, TG_SAVE);
        }
    }

    private async Task DB_SaveTG
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
        (int id, DateTime date) => $"{id}-{new DateTimeOffset(date).ToUnixTimeSeconds()}.webp";
}
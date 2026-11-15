using System.Threading.Tasks.Dataflow;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.Core.Search;
using MemeIndex.DB;
using Microsoft.Data.Sqlite;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private async Task CA_Start(StatusScope status)
    {
        const string CODE = "FP-CA";
        Log(CODE, "START");

        var db_files = await _con.Files_GetToBeAnalyzed();
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

        var b_load    = new TransformBlock<IX_Ctx, IX_Ctx> (FP_Load,    exec_ops_4cores_16files);
        var b_analyze = new TransformBlock<IX_Ctx, DB_Task>(CA_Analyze, exec_ops_4cores_16files);
        b_load   .LinkTo( b_analyze, link_ops_propagateCompletion);
        b_analyze.LinkTo(_b_batch);
        Log(CODE, "DEFINE BLOCKS");

        ImagePool.Book(files.Select(x => x.Path), files.Length);

        foreach (var file in files)
        {
            if ((await b_load.SendAsync(file)).Failed())
                LogWarn(CODE, $"FAILED TO SEND FILE {file.Id}");
        }
        Log(CODE, "SEND FILES");

        b_load.Complete();
        await b_analyze.Completion;
        
        Log(CODE, "DONE");
    }

    private DB_Task CA_Analyze(IX_Ctx ix_ctx)
    {
        var ctx = new CA_Ctx(ix_ctx);
        // Log("IX_Analyze", $"{ctx.Id}");
        Tracer.LogOpen(ctx.Id, CA_SCAN);
        var report = ColorAnalyzer_v2.ScanImage(ctx.Image);
        ctx.Image = null!;
        ImagePool.Return(ctx.Path);
        Tracer.LogJoin(ctx.Id, CA_SCAN, CA_ANAL);
        var tags = ColorTagger_v2.AnalyzeImageScan(report, minScore: 10);
        ctx.DB_File = new DB_File_UpdateDate(ctx.Id, DateTime.UtcNow);
        ctx.DB_Tags = tags
            .Select(x => new TagContent(x.Key, x.Value))
            .Select(x => new DB_Tag_Insert(x, ctx.Id));
        Tracer.LogDone(ctx.Id, CA_ANAL);
        return (con, tx) => IX_WriteCA(ctx, con, tx);
    }

    private async Task IX_WriteCA
        (CA_Ctx ctx, SqliteConnection con, SqliteTransaction tx)
    {
        Tracer.LogOpen(ctx.Id, DB_W_TAGS);
        await con.Tags_CreateMany(ctx.DB_Tags, tx);
        Tracer.LogJoin(ctx.Id, DB_W_TAGS, DB_W_FA);
        Jarvis.Cache_Clear();
        await con.File_UpdateDateAnalyzed(ctx.DB_File, tx);
        Tracer.LogDone(ctx.Id, DB_W_FA);
    }
}
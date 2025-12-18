using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using MemeIndex.Utils;
using Microsoft.Data.Sqlite;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private static readonly ImagePool ImagePool = new();
    
    private readonly Channel<Func<SqliteConnection, Task>>
        C_DB_Write = Channel.CreateUnbounded<Func<SqliteConnection, Task>>();

    private Job_DB_Write?         job_DB;
    private Job_ThumbgenSaveWebp? job_thumbsWebp;

    [MethodImpl(Synchronized)]
    private Job_DB_Write? InitJob_DB_Write()
        => job_DB == null
        || job_DB.ExecuteTask is { IsCompleted: true }
            ? job_DB = new Job_DB_Write(C_DB_Write, Tracer)
            : null;

    public async Task Run()
    {
        // LAUNCH TASKS (they create necessary jobs)
        await Task.WhenAll(GenerateThumbnails(), AnalyzeFiles());

        // WAIT FOR [OTHER] JOBS TO FINISH
        var jobTasks = new [] { job_thumbsWebp }
            .Select(x => x?.ExecuteTask)
            .OfType<Task>();
        await Task.WhenAll(jobTasks);

        // WAIT FOR [DB WRITER] JOB TO FINISH
        C_DB_Write.Writer.Complete();
        if (null != job_DB?.ExecuteTask)
            await   job_DB .ExecuteTask;

        SaveTraceData();
    }

    // ANALYZE

    private async Task AnalyzeFiles()
    {
        const string CODE = "AnalyzeFiles";
        Log(CODE, "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var db_files = await con.Files_GetToBeAnalyzed();
        await con.CloseAsync();
        Log(CODE, "GET FILES");

        var files = db_files.Select(x => x.Compile()).ToArray();
        if (files.Length == 0)
        {
            Log(CODE, "NOTHING TO PROCESS");
            return;
        }

        ImagePool.Book(files.Select(x => x.Path), files.Length);

        if (InitJob_DB_Write() is { } job)
            await job.StartAsync(CancellationToken.None);

        foreach (var file in files)
        {
            try
            {
                await AnalyzeImage(file.Id, file.Path);
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }
        Log(CODE, "DONE");
    }

    private async Task AnalyzeImage
        (int id, string path, int minScore = 10)
    {
        Tracer.LogOpen(id, CA_LOAD);
        var image = await ImagePool.Load(path);
        Tracer.LogJoin(id, CA_LOAD, CA_SCAN);
        var report = ColorAnalyzer_v2.ScanImage(image);
        ImagePool.Return(path);
        Tracer.LogJoin(id, CA_SCAN, CA_ANAL);
        var tags = ColorTagger_v2.AnalyzeImageScan(report, minScore);
        Tracer.LogDone(id, CA_ANAL);
        LogDebug($"File {id,6} -> color analysis done");

        var db_file = new DB_File_UpdateDate(id, DateTime.UtcNow);
        var db_tags = tags
            .Select(x => new TagContent(x.Key, x.Value))
            .Select(x => new DB_Tag_Insert(x, id));

        await C_DB_Write.Writer.WriteAsync(async connection =>
        {
            Tracer.LogOpen(id, DB_W_TAGS);
            await connection.Tags_CreateMany        (db_tags);
            Tracer.LogJoin(id, DB_W_TAGS, DB_W_FA);
            await connection.File_UpdateDateAnalyzed(db_file);
            Tracer.LogDone(id, DB_W_FA);
        });
    }

    // STATS

    private readonly TraceCollector Tracer = new();

    public const string // LANES
        THUMB_LOAD = "1. Thumbnail / Load",
        THUMB_SIZE = "2. Thumbnail / Resize",
        THUMB_SAVE = "3. Thumbnail / Save",
        CA_LOAD    = "4. Color Analysis / Load",
        CA_SCAN    = "5. Color Analysis / Scan",
        CA_ANAL    = "6. Color Analysis / Analyze",
        DB_WRITE   = "7. DB Write",
        DB_W_TAGS  = "8. DB Write / Tags",
        DB_W_FA    = "9. DB Write / File Analysis",
        DB_W_FT    = "A. DB Write / File Thumbgen";

    private void SaveTraceData()
    {
        if (Tracer.Empty) return;

        var c1 = Tracer.Count(THUMB_LOAD);
        var c2 = Tracer.Count(   CA_LOAD);
        var save = Dir_Traces
            .EnsureDirectoryExist()
            .Combine($"File-processing-{Desert.Clock(24):x}_{Helpers.COMPILE_MODE}_{c1}-{c2}.json");
        Tracer.SaveAs(save, AppJson.Default.DictionaryStringListTraceSpan);
        Tracer.PrintStats();
        Log($"Save trace data - \"{save}\"");
    }
}
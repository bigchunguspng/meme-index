using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using MemeIndex.Utils;
using Microsoft.Data.Sqlite;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private readonly Channel<Func<SqliteConnection, Task>>
        C_DB_Write = Channel.CreateUnbounded<Func<SqliteConnection, Task>>();

    private static readonly ImagePool ImagePool = new();

    public async Task Run()
    {
        // START JOBS
        var job_DB = new Job_DB_Write(C_DB_Write, Tracer);
        var jobs = new BackgroundService[]
        {
            job_DB,
            //new Job_ThumbgenResize(this),
            new Job_ThumbgenSaveWebp(this),
        };
        foreach (var job in jobs)
        {
            await job.StartAsync(CancellationToken.None);
        }

        // LAUNCH TASKS
        _ = GenerateThumbnails();
        _ = AnalyzeFiles();

        // WAIT FOR JOBS TO FINISH
        var jobTasks = jobs
            .Skip(1)
            .Select(x => x.ExecuteTask)
            .OfType<Task>();
        await Task.WhenAll(jobTasks);

        // WAIT FOR DB WRITER JOB TO FINISH
        C_DB_Write.Writer.Complete();
        if (null != job_DB.ExecuteTask)
            await   job_DB.ExecuteTask;

        SaveTraceData();
    }

    // ANALYZE

    private async Task AnalyzeFiles()
    {
        Log("AnalyzeFiles", "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var db_files = await con.Files_GetToBeAnalyzed();
        await con.CloseAsync();
        Log("AnalyzeFiles", "GET FILES");

        var files = db_files.Select(x => x.Compile()).ToArray();
        ImagePool.Book(files.Select(x => x.Path), files.Length);

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
        Log("AnalyzeFiles", "DONE");
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
#if AOT
        const string mode = "AOT";
#else
        const string mode = "JIT";
#endif
        var save = Dir_Traces
            .EnsureDirectoryExist()
            .Combine($"File-processing-{Desert.Clock(24):x}_{mode}.json");
        Tracer.SaveAs(save, AppJson.Default.DictionaryStringListTraceSpan);
        Tracer.PrintStats();
        Log($"Save trace data - \"{save}\"");
    }
}
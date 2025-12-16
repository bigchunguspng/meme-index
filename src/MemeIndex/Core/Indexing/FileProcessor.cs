using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using MemeIndex.Utils;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;

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
        var files = await con.Files_GetToBeAnalyzed();
        await con.CloseAsync();
        Log("AnalyzeFiles", "GET FILES");

        var filesIP = files
            .Select(x => new { Id = x.id, Path = x.GetPath() })
            .ToArray();
        ImagePool.Book(filesIP.Select(x => x.Path), filesIP.Length);

        foreach (var file in filesIP)
        {
            try
            {
                var tags = await AnalyzeImage(file.Id, file.Path);
                Log($"Analyze file {file.Id,5}");

                var db_tags = tags.Select(x => new DB_Tag_Insert(x, file.Id));
                var db_file = new DB_File_UpdateDate(file.Id, DateTime.UtcNow);

                await C_DB_Write.Writer.WriteAsync(async connection =>
                {
                    Tracer.LogStart(DB_W_TAGS, file.Id);
                    await connection.Tags_CreateMany        (db_tags);
                    Tracer.LogBoth (DB_W_TAGS, file.Id, DB_W_FA);
                    await connection.File_UpdateDateAnalyzed(db_file);
                    Tracer.LogEnd  (DB_W_FA,   file.Id);
                });
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }
        Log("AnalyzeFiles", "DONE");
    }

    private async Task<IEnumerable<TagContent>> AnalyzeImage
        (int id, string path, int minScore = 10)
    {
        Tracer.LogStart(CA_LOAD, id);
        var image = await ImagePool.Load(path);
        Tracer.LogBoth (CA_LOAD, id, CA_SCAN);
        var report = ColorAnalyzer_v2.ScanImage(image);
        ImagePool.Return(path);
        Tracer.LogBoth (CA_SCAN, id, CA_ANAL);
        var tags = ColorTagger_v2.AnalyzeImageScan(report, minScore);
        Tracer.LogEnd  (CA_ANAL, id);
        return tags
            .Select(x => new TagContent(x.Key, x.Value));
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
        var mode = SystemHelpers.IsAOT() ? "AOT" : "JIT";
        var save = Dir_Traces
            .EnsureDirectoryExist()
            .Combine($"File-processing-{Desert.Clock(24):x}_{mode}.json");
        Tracer.SaveAs(save, AppJson.Default.DictionaryStringListTraceSpan);
        Tracer.PrintStats();
        Log($"Save trace data - \"{save}\"");
    }
}
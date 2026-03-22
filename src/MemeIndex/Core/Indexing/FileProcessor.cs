using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using MemeIndex.Utils;
using Microsoft.Data.Sqlite;

namespace MemeIndex.Core.Indexing;

public partial class FileProcessor
{
    private static readonly ImagePool ImagePool = new();

    public async Task Run()
    {
        // LAUNCH TASKS (they create necessary jobs)
        await Task.WhenAll(StartThumbnailGeneration(), StartColorAnalysis());

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

    // DB WRITE

    private readonly Channel<Func<SqliteConnection, Task>>
        C_DB_Write = Channel.CreateUnbounded<Func<SqliteConnection, Task>>();

    private Job_DB_Write? job_DB;

    [MethodImpl(Synchronized)]
    private Job_DB_Write? InitJob_DB_Write()
        => job_DB == null
        || job_DB.ExecuteTask is { IsCompleted: true }
            ? job_DB = new Job_DB_Write(C_DB_Write, Tracer)
            : null;

    /// DB writes are done in batches via this job.
    public class Job_DB_Write
    (
        Channel<Func<SqliteConnection, Task>> channel,
        TraceCollector tracer
    ) : BackgroundService
    {
        private const string code = "Job/DB-Writer";
        private readonly List<Func<SqliteConnection, Task>> _queue = new(16);

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            Log(code, "STARTED");
            await foreach (var task in channel.Reader.ReadAllAsync(ct))
            {
                if (_queue.Count == _queue.Capacity)
                {
                    await ProcessQueue();
                    _queue.Clear();
                }

                _queue.Add(task);
            }

            if (_queue.Count > 0)
                await ProcessQueue();

            Log(code, "COMPLETED");
        }

        private int id = 10_000;
        private async Task ProcessQueue()
        {
            tracer.LogOpen(id, DB_WRITE);
            await using var con = await AppDB.ConnectTo_Main();
            foreach (var task in _queue)
            {
                await task(con);
            }
            await con.CloseAsync();
            tracer.LogDone(id++, DB_WRITE);
            Log(code, $"Processed {_queue.Count} items!");
        }
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
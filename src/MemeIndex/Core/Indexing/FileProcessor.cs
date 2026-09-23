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

        await SaveTraceData();
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
            try
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
            catch (Exception e)
            {
                App.LogException_JOB(e);
                throw;
            }
        }

        private int task_id = 1;

        private async Task ProcessQueue()
        {
            tracer.LogOpen(task_id, DB_WRITE);
            await using var con = await AppDB.ConnectTo_Main();
            foreach (var task in _queue)
            {
                await task(con);
            }
            await con.CloseAsync();
            tracer.LogDone(task_id++, DB_WRITE);
            Log(code, $"Processed {_queue.Count} items!");
        }
    }

    // STATS

    public const int // LANES
        TG_LOAD   = 0x1,
        TG_SIZE   = 0x2,
        TG_SAVE   = 0x3,
        CA_LOAD   = 0x4,
        CA_SCAN   = 0x5,
        CA_ANAL   = 0x6,
        DB_WRITE  = 0x7,
        DB_W_TAGS = 0x8,
        DB_W_FA   = 0x9,
        DB_W_FT   = 0xA;

    private readonly TraceCollector Tracer = new
    ([
        ("TG / Load",          "File Id"),
        ("TG / Resize",        "File Id"),
        ("TG / Save",          "File Id"),
        ("CA / Load",          "File Id"),
        ("CA / Scan",          "File Id"),
        ("CA / Analyze",       "File Id"),
        ("DB Write",           "Write #"), // batch writes
        ("DB Write / Tags",    "File Id"), // < add tags
        ("DB Write / File CA", "File Id"),
        ("DB Write / File TG", "File Id"), // <^ update dates
    ]);

    private async Task SaveTraceData()
    {
        if (Tracer.Empty) return;

        var c1 = Tracer.Count(TG_LOAD);
        var c2 = Tracer.Count(CA_LOAD);
        var save = Dir_Traces
            .EnsureDirectoryExist()
            .Combine($"File-processing-{Desert.Clock(24):x}_{Helpers.COMPILE_MODE}_TG-{c1}_CA-{c2}.txt");
        await Tracer.SaveAs(save);
        Tracer.PrintStats();
        Log($"Save trace data - \"{save}\"");
    }
}
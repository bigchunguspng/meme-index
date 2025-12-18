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

    public readonly     Channel<ThumbgenContext>
      //C_TG_Resize   = Channel.CreateUnbounded<ThumbgenContext>(),
        C_TG_SaveWebp = Channel.CreateUnbounded<ThumbgenContext>();

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
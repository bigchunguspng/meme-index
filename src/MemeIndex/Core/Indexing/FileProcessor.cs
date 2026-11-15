using System.Threading.Tasks.Dataflow;
using MemeIndex.DB;
using MemeIndex.Utils;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Indexing;

// TODO:
/*  - handle db write errors:
        catch (Exception e)
        {
            App.LogException_JOB(e, "DB WRITE");
            throw;
        }
        
        foreach (var file in files)
        {
            try
            {
                await AnalyzeImage_Color(file.Id, file.Path);
            }
            catch (Exception e)
            {
                LogError(e);
                await MarkBrokenFile(file.Id, "color analysis");
            }
        }
        
    - replace channels with other system (FP, Sync):
        trigger while idle -> start work
        trigger while busy -> start work again when done (recheck)
    - handle errors in other jobs:
        FP, Sync, FP-*
    - mark broken files
    [optional]:
    - get files, unite lists, book once, process
*/

public delegate Task DB_Task(SqliteConnection con, SqliteTransaction tx);

public partial class FileProcessor
{
    private const string NAME = "FP";

    public FileProcessor()
    {
        Tracer    = CreateTracer();
        ImagePool = new ImagePool(Tracer, IP_LOAD);
    }

    private SqliteConnection _con;

    private  BatchBlock<DB_Task>   _b_batch;
    private ActionBlock<DB_Task[]> _b_write;

    /// Processes (OCR, Color analysis, thumbnail generation)
    /// all the files that needs to be processed,
    /// e.g. ones with no/outdated processing date.
    public async Task Run()
    {
        const string CODE = "FP";
        Log(CODE, "START");

        await using var status = new StatusScope("PROCESSING");
        await using var con = _con = await AppDB.ConnectTo_Main();

        _b_batch = new  BatchBlock<DB_Task>(16);
        _b_write = new ActionBlock<DB_Task[]>(DB_BatchWrite);
        _b_batch.LinkTo(_b_write, link_ops_propagateCompletion);
        Log(CODE, "DEFINE BLOCKS");

        var task1 = CA_Start(status);
        var task2 = TG_Start(status);
        Log(CODE, "START TASKS");

        await Task.WhenAll(task1, task2);
        Log(CODE, "AWAIT TASKS");

        _b_batch.Complete();
        await _b_write.Completion;
        await con.CloseAsync();
        Log(CODE, "COMPLETE");

        await SaveTraceData();
    }

    // LOAD

    private readonly ImagePool ImagePool;

    private async Task<IX_Ctx> FP_Load(IX_Ctx ctx)
    {
        // Log("IX_Load", $"{ctx.Id}");
        ctx.Image = await ImagePool.Load(ctx.Path, ctx.Id);
        return ctx;
    }

    // DB

    private int _db_write_id = 1;

    private async Task DB_BatchWrite
        (DB_Task[] tasks)
    {
        Log(NAME, $"IX_DB_BatchWrite -> exec {tasks.Length} commands");
        Tracer.LogOpen(_db_write_id, DB_WRITE);
        await using var tx = _con.BeginTransaction();
        foreach (var task in tasks)
        {
            await task(_con, tx);
        }
        await tx.CommitAsync();
        Tracer.LogDone(_db_write_id++, DB_WRITE);
    }

    private DB_Task IX_MarkBrokenFile(int id, string action_name)
    {
        LogDebug($"File {id,6} -> BROKEN ({action_name})");
        return (con, tx) => IX_MarkBrokenFile(id, con, tx);
    }

    private async Task IX_MarkBrokenFile
        (int id, SqliteConnection con, SqliteTransaction tx)
    {
        await con.FileBroken_Create(id, tx);
    }

    // OPTIONS

    private static readonly ExecutionDataflowBlockOptions exec_ops_4cores_16files = new()
    {
        EnsureOrdered = true,
        BoundedCapacity = 16,
        MaxDegreeOfParallelism = 4,
    };

    private static readonly DataflowLinkOptions link_ops_propagateCompletion = new()
    {
        PropagateCompletion = true,
    };

    // FILE PROCESSING JOB

    private static readonly Jack<int, Job_FileProcessing> Jack_FileProcessing = new();

    public class Job_FileProcessing()
        : ChannelJob_ExecuteOrStop
        (
            "Job/FileProcessing",
            Jack_FileProcessing.Chan,
            async () => await new FileProcessor().Run(),
            "TASK DONE!",
            e => App.LogException_JOB(e, "FILE PROCESSING")
        );

    public static async Task Trigger()
    {
        _   = Jack_FileProcessing.Chan.Writer.TryWrite(1);
        await Jack_FileProcessing.EnsureJobStarted();
    }

    // STATS

    public const int // LANES
        IP_LOAD   = 0x1,
        TG_SIZE   = 0x2,
        TG_SAVE   = 0x3,
        CA_SCAN   = 0x4,
        CA_ANAL   = 0x5,
        DB_WRITE  = 0x6,
        DB_W_TAGS = 0x7,
        DB_W_FA   = 0x8,
        DB_W_FT   = 0x9;

    private readonly TraceCollector       Tracer;
    private static   TraceCollector CreateTracer() => new
    ([
        ("Image Load",         "File Id"),
        ("TG / Resize",        "File Id"),
        ("TG / Save",          "File Id"),
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

        var ca = Tracer.Count(DB_W_FA);
        var tg = Tracer.Count(DB_W_FT);
        var save = Dir_Traces
            .EnsureDirectoryExist()
            .Combine($"File-processing-{Desert.Clock(24):x}_{Helpers.COMPILE_MODE}_TG-{tg}_CA-{ca}.txt");
        await Tracer.SaveAs(save);
        Tracer.PrintStats();
        Log($"Save trace data - \"{save}\"");
    }
}

public struct IX_Ctx
{
    public int                        Id      { get; set; }
    public string                     Path    { get; set; }
    public Image<Rgba32>              Image   { get; set; }
}
public struct TG_Ctx(IX_Ctx ctx)
{
    public int                        Id      { get; set; } = ctx.Id;
    public string                     Path    { get; set; } = ctx.Path;
    public Image<Rgba32>              Image   { get; set; } = ctx.Image;
    public Image<Rgba32>              Thumb   { get; set; }
    public Size                       OG_Size { get; set; }
    public DB_File_UpdateDateSize     DB_File { get; set; }
}
public struct CA_Ctx(IX_Ctx ctx)
{
    public int                        Id      { get; set; } = ctx.Id;
    public string                     Path    { get; set; } = ctx.Path;
    public Image<Rgba32>              Image   { get; set; } = ctx.Image;
    public DB_File_UpdateDate         DB_File { get; set; }
    public IEnumerable<DB_Tag_Insert> DB_Tags { get; set; }
}
using System.Threading.Channels;
using MemeIndex.DB;
using Microsoft.Data.Sqlite;

namespace MemeIndex.Core.Indexing;

public abstract class ChannelJob<T>
(
    string code,
    Channel<T> channel,
    Func<T, Task> process_item,
    Func<T, string>? log_item = null,
    Channel<T>? channelToComplete = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(code, "STARTED");
        await foreach (var item in channel.Reader.ReadAllAsync(ct))
        {
            await process_item(item);

            if (log_item != null)
                Log(code, log_item(item));
        }
        if (channelToComplete != null)
            channelToComplete.Writer.Complete();
        Log(code, "COMPLETED");
    }
}

public abstract class ChannelJob_X
(
    string code,
    Channel<int> channel,
    Func<Task> execute,
    string? log_string = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Log(code, "STARTED");
        await foreach (var _ in channel.Reader.ReadAllAsync(ct))
        {
            await execute();

            if (log_string != null)
                Log(code, log_string);
        }
        Log(code, "COMPLETED");
    }
}

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

        await ProcessQueue();
        Log(code, "COMPLETED");
    }

    private int id = 10_000;
    private async Task ProcessQueue()
    {
        tracer.LogStart(FileProcessor.DB_WRITE, id);
        await using var con = await AppDB.ConnectTo_Main();
        foreach (var task in _queue)
        {
            await task(con);
        }
        await con.CloseAsync();
        tracer.LogEnd  (FileProcessor.DB_WRITE, id++);
        Log(code, $"Processed {_queue.Count} items!");
    }
}

//

public class Job_FileProcessing()
    : ChannelJob_X
    (
        "Job/FileProcessing",
        Command_AddFilesToDB.C_FileProcessing,
        async () =>
        {
            await new FileProcessor().Run();
        },
        "Task done!"
    );

//

/*public class Job_ThumbgenResize(FileProcessingTask task)
    : ChannelJob<ThumbgenContext>
    (
        "Job/Thumbgen-Resize",
        task.C_Resize,
        task.Thumbnail_Resize
    );*/

public class Job_ThumbgenSaveWebp(FileProcessor task)
    : ChannelJob<ThumbgenContext>
    (
        "Job/Thumbgen-Save-Webp",
        task.C_SaveWebp,
        task.Thumbnail_Save
    );

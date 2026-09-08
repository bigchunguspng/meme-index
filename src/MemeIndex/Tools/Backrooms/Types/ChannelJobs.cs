using System.Threading.Channels;

namespace MemeIndex.Tools.Backrooms.Types;

public abstract class ChannelJob<T>
(
    string code,
    Channel<T> channel,
    Func<T, Task> process_item,
    Func<T, string>? log_item = null,
    Channel<T>? channelToComplete = null,
    Action<Exception>? exceptionHandler = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
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
        catch (Exception e)
        {
            exceptionHandler?.Invoke(e);
            throw;
        }
    }
}

public abstract class ChannelJob_Execute
(
    string code,
    Channel<int> channel,
    Func<Task> execute,
    string? log_string = null,
    Action<Exception>? exceptionHandler = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
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
        catch (Exception e)
        {
            exceptionHandler?.Invoke(e);
            throw;
        }
    }
}

public abstract class ChannelJob_ExecuteOrStop
(
    string code,
    Channel<int> channel,
    Func<Task> execute,
    string? log_string = null,
    Action<Exception>? exceptionHandler = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            Log(code, "STARTED");
            while (channel.Reader.TryRead(out _))
            {
                await execute();

                if (log_string != null)
                    Log(code, log_string);
            }
            Log(code, "COMPLETED");
        }
        catch (Exception e)
        {
            exceptionHandler?.Invoke(e);
            throw;
        }
    }
}
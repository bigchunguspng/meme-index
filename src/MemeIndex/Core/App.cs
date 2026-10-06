using System.Threading.Channels;

namespace MemeIndex.Core;

public static class App
{
    public static AppDomain Domain => AppDomain.CurrentDomain;

    public static readonly FileLogger_Batch  Logger_Log = new(File_Log);
    public static readonly FileLogger_Simple Logger_Err = new(File_Err);

    public static string DefaultStatus => "IDLE";
    public static string?       Status { get; private set; }

    public static readonly Channel<string?>
        C_Events         = Channel.CreateUnbounded<string?>();

    public static ValueTask SetStatus(string? message = null)
    {
        Status = message;
        return C_Events.Writer.WriteAsync(message);
    }

    public static void SaveAndExit()
    {
        Logger_Log.Write();
    }

    public static void LogException_JOB(Exception e)
    {
        LogError(e);
        LogException(e, ExceptionCategory.JOB);
    }

    public static void LogException
        (Exception e, ExceptionCategory c, string context = "N/A")
    {
        try
        {
            var entry =
                $"""
                 @ {DateTime.Now:yyyy-MM-dd ddd', 'HH:mm:ss.fff}
                 Category | {c}
                 Context  | {context}
                 {e}
                 
                 
                 """;
            Logger_Err.Log(entry);
        }
        catch
        {
            Console.WriteLine("EXCEPTION WHILE LOGGING EXCEPTION x_x");
        }
    }
}

public enum ExceptionCategory
{
    CRASH, API, JOB,
}

public class StatusScope(string? status) : IAsyncDisposable
{
    public string? Status => status;
    public TimeSpan Delay;

    public static async Task<StatusScope> Enter
        (string? status)
    {
        await App.SetStatus(status);
        return new StatusScope(status);
    }

    public async ValueTask DisposeAsync()
    {
        if (App.Status == status)
        {
            if (Delay != TimeSpan.Zero)
                await Task.Delay(Delay);

            await App.SetStatus();
        }
    }
}
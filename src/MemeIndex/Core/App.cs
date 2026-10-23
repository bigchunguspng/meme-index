using System.Threading.Channels;
using MemeIndex.Core.Indexing;

namespace MemeIndex.Core;

public static class App
{
    public static AppDomain Domain => AppDomain.CurrentDomain;

    public static readonly FileLogger_Batch  Logger_Log = new(File_Log);
    public static readonly FileLogger_Simple Logger_Err = new(File_Err);

    // STATUS

    public static string DefaultStatus => "IDLE";
    public static string?       Status { get; private set; }

    public static readonly Channel<string?>
        C_Events         = Channel.CreateUnbounded<string?>();

    public static void SetStatus(string? message = null)
    {
        Status = message;
        C_Events.Writer.TryWrite(message);
    }

    // START

    public static async Task Start()
    {
        await Miku.Start();
        if (Config.StartupSync_Enabled)
            _ = Indexing.Indexing.TriggerSyncAfter(Config.StartupSync_Delay);
    }

    // EXIT

    private static bool terminating;

    public static void SaveAndExit()
    {
        if (Interlocked.Exchange(ref terminating, true))
            return;

        Task.WhenAll(Miku.Stop(), Logger_Log.Write()).Wait();
    }

    // LOG

    public static void LogException_JOB(Exception e, string ctx)
    {
        LogError(e);
        LogException(e, ExceptionCategory.JOB, ctx);
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

    public static string GetCrashContext() =>
        $"""
         …
            File:              {Environment.ProcessPath}
            Command line:      {Environment.CommandLine}
            Working directory: {Environment.CurrentDirectory}
         """;
}

public enum ExceptionCategory
{
    CRASH, API, JOB,
}

public class StatusScope(string? status) : IAsyncDisposable
{
    public string? Status => status;
    public TimeSpan Delay;

    public static StatusScope Enter
        (string? status)
    {
        App.SetStatus(status);
        return new StatusScope(status);
    }

    public async ValueTask DisposeAsync()
    {
        if (App.Status == status)
        {
            if (Delay != TimeSpan.Zero)
                await Task.Delay(Delay);

            App.SetStatus();
        }
    }
}
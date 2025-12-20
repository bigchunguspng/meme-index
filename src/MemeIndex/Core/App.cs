namespace MemeIndex.Core;

public static class App
{
    public static AppDomain Domain => AppDomain.CurrentDomain;

    public static readonly FileLogger_Batch  Logger_Log = new(File_Log);
    public static readonly FileLogger_Simple Logger_Err = new(File_Err);

    public static void SaveAndExit()
    {
        Logger_Log.Write();
    }

    public static void LogException(Exception e, ExceptionCategory c, string context = "N/A")
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
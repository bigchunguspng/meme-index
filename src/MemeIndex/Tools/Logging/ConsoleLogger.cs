using System.Runtime.CompilerServices;

namespace MemeIndex.Tools.Logging;

public static class ConsoleLogger
{
    // PRINT

    [MethodImpl(Synchronized)]
    public static void Print
        (string message) =>
        Console.WriteLine(message);

    [MethodImpl(Synchronized)]
    public static void Print
        (string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    // LOG (MORE FANCY)

    private static DateTime _prevDate = DateTime.UtcNow;

    private static void Console_WritePrefix(LogLevel level)
    {
        var         thisDate = DateTime.UtcNow;
        var delta = thisDate - _prevDate;
        _prevDate = thisDate;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{DateTime.Now:MMM' 'dd', 'HH:mm:ss.fff} ");
        Console.ForegroundColor = level.GetDefaultColor();
        Console.Write($"{level.GetCharIcon()} ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{delta.ReadableTime(),-10} | ");
    }

    private static char GetCharIcon
        (this LogLevel level) => level switch
    {
        LogLevel.Debug => 'D',
        LogLevel.Info  => '#',
        LogLevel.Warn  => 'W',
        LogLevel.Error => 'E',
        _              => '?',
    };

    private static ConsoleColor GetDefaultColor
        (this LogLevel level) => level switch
    {
        LogLevel.Debug => ConsoleColor.Blue,
        LogLevel.Info  => ConsoleColor.Green,
        LogLevel.Warn  => ConsoleColor.Red,
        LogLevel.Error => ConsoleColor.Red,
        _              => ConsoleColor.Gray,
    };

    //

    public static void LogError
        (Exception ex,   ConsoleColor color = ConsoleColor.DarkRed)
        => Log($"{ex}", LogLevel.Error, color);

    public static void LogError
        (string message, ConsoleColor color = ConsoleColor.DarkRed)
        => Log(message, LogLevel.Error, color);

    public static void LogWarn
        (string message, ConsoleColor color = ConsoleColor.DarkYellow)
        => Log(message, LogLevel.Warn, color);

    public static void LogDebug
        (string message, ConsoleColor color = ConsoleColor.Blue)
        => Log(message, LogLevel.Debug, color);

    [MethodImpl(Synchronized)]
    public static void Log
    (
        string message,
        LogLevel level = LogLevel.Info,
        ConsoleColor color = ConsoleColor.Gray
    )
    {
        Console_WritePrefix(level);
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    [MethodImpl(Synchronized)]
    public static void Log
    (
        string category,
        string message,
        LogLevel level = LogLevel.Info,
        ConsoleColor color = ConsoleColor.Gray
    )
    {
        Console_WritePrefix(level);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{category} ");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    [MethodImpl(Synchronized)]
    public static void Log
    (
        this Stopwatch sw,
        string message,
        ConsoleColor color = ConsoleColor.Blue
    )
    {
        sw.Stop();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"0x{sw.GetHashCode():X8}    [TIME] ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("T ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{sw.ElapsedReadable(),-10} @ ");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
        sw.Restart();
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace MemeIndex.Tools.Logging;

public static class ConsoleLogger
{
    // PRINT

    [MethodImpl(AggressiveInlining)]
    public static void Print
        (string message) =>
        Console.WriteLine(message);

    [MethodImpl(AggressiveInlining)]
    public static void Print
        (string message, ConsoleColor color) =>
        Console.WriteLine($"\e[{color.ToANSI()}m{message}\e[0m");

    // LOG (MORE FANCY)

    public  static LogLevel MinLogLevel = LogLevel.Debug;
    private static long _prevDate= DateTime.UtcNow.Ticks;

    private static StringBuilder Console_Prefix(this StringBuilder sb, LogLevel level)
    {
        var thisDate = DateTime.UtcNow.Ticks;
        var prevDate = Interlocked.Exchange(ref _prevDate, thisDate);
        var delta    = TimeSpan.FromTicks(thisDate - prevDate);
        var color    = level.GetDefaultColor();
        return sb
            .Color(CC_DARK_GRAY).Append($"{DateTime.Now:MMM' 'dd', 'HH:mm:ss.fff} ")
            .Color(color       ).Append(level.GetCharIcon()).Append(' ')
            .Color(CC_DARK_GRAY).Append($"{delta.ReadableTime(),10} | ");
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

    // LOG > PUBLIC

    [MethodImpl(AggressiveInlining)]
    public static void LogError
        (Exception ex,   ConsoleColor color = ConsoleColor.DarkRed)
        => Log($"{ex}", LogLevel.Error, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogError
        (string message, ConsoleColor color = ConsoleColor.DarkRed)
        => Log(message, LogLevel.Error, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogWarn
        (string message, ConsoleColor color = ConsoleColor.DarkYellow)
        => Log(message, LogLevel.Warn, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogDebug
        (string message, ConsoleColor color = ConsoleColor.Blue)
        => Log(message, LogLevel.Debug, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogCM // CM = color, message
        (ConsoleColor color, string message, LogLevel level = LogLevel.Info)
        => Log(message, level, color);

    // LOG > PUBLIC (with category)

    [MethodImpl(AggressiveInlining)]
    public static void LogError
        (string category, Exception ex,   ConsoleColor color = ConsoleColor.DarkRed)
        => Log(category, $"{ex}", LogLevel.Error, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogError
        (string category, string message, ConsoleColor color = ConsoleColor.DarkRed)
        => Log(category, message, LogLevel.Error, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogWarn
        (string category, string message, ConsoleColor color = ConsoleColor.DarkYellow)
        => Log(category, message, LogLevel.Warn, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogDebug
        (string category, string message, ConsoleColor color = ConsoleColor.Blue)
        => Log(category, message, LogLevel.Debug, color);

    [MethodImpl(AggressiveInlining)]
    public static void LogCM // CM = color, message
        (string category, ConsoleColor color, string message, LogLevel level = LogLevel.Info)
        => Log(category, message, level, color);

    // LOG GENERAL

    public static void Log
    (
        string message,
        LogLevel level = LogLevel.Info,
        ConsoleColor color = ConsoleColor.Gray
    )
    {
        if (level < MinLogLevel) return;
        using var psb = new PooledStringBuilder();
        psb.sb.Console_Prefix(level)
            .Color(color).Append(message).Style().Print();
    }

    public static void Log
    (
        string category, string message,
        LogLevel level = LogLevel.Info,
        ConsoleColor color = ConsoleColor.Gray
    )
    {
        if (level < MinLogLevel) return;
        using var psb = new PooledStringBuilder();
        psb.sb.Console_Prefix(level)
            .Color(CC_YELLOW).Append(category).Append(' ')
            .Color(color).Append(message).Style().Print();
    }

    // LOG STOPWATCH

    [MethodImpl(AggressiveInlining)]
    public static void LogCM // CM = color, message
        (this Stopwatch sw, ConsoleColor color, string message)
        => sw.Log(message, color);

    public static void Log
    (
        this Stopwatch sw, string message,
        ConsoleColor color = ConsoleColor.Blue
    )
    {
        sw.Stop();
        if (MinLogLevel <= LogLevel.Debug)
        {
            using var psb = new PooledStringBuilder();
            psb.sb
                .Color(CC_DARK_GRAY).Append($"0x{sw.GetHashCode():x8}    [TIME] ")
                .Color(CC_YELLOW   ).Append("T ")
                .Color(CC_DARK_GRAY).Append($"{sw.ElapsedReadable(),-10} * ")
                .Color(color).Append(message)
                .Style().Print();
        }
        sw.Restart();
    }

    // COLOR

    public const int
        // DARK
        CC_BLACK        = 30,
        CC_DARK_RED     = 31,
        CC_DARK_GREEN   = 32,
        CC_DARK_YELLOW  = 33,
        CC_DARK_BLUE    = 34,
        CC_DARK_MAGENTA = 35,
        CC_DARK_CYAN    = 36,
        CC_GRAY         = 37,
        // BRIGHT
        CC_DARK_GRAY    = 90,
        CC_RED          = 91,
        CC_GREEN        = 92,
        CC_YELLOW       = 93,
        CC_BLUE         = 94,
        CC_MAGENTA      = 95,
        CC_CYAN         = 96,
        CC_WHITE        = 97;

    public static int ToANSI(this ConsoleColor color) => color switch
    {
        // DARK
        ConsoleColor.Black       => CC_BLACK,
        ConsoleColor.DarkRed     => CC_DARK_RED,
        ConsoleColor.DarkGreen   => CC_DARK_GREEN,
        ConsoleColor.DarkYellow  => CC_DARK_YELLOW,
        ConsoleColor.DarkBlue    => CC_DARK_BLUE,
        ConsoleColor.DarkMagenta => CC_DARK_MAGENTA,
        ConsoleColor.DarkCyan    => CC_DARK_CYAN,
        ConsoleColor.Gray        => CC_GRAY,
        // BRIGHT
        ConsoleColor.DarkGray    => CC_DARK_GRAY,
        ConsoleColor.Red         => CC_RED,
        ConsoleColor.Green       => CC_GREEN,
        ConsoleColor.Yellow      => CC_YELLOW,
        ConsoleColor.Blue        => CC_BLUE,
        ConsoleColor.Magenta     => CC_MAGENTA,
        ConsoleColor.Cyan        => CC_CYAN,
        ConsoleColor.White       => CC_WHITE,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null),
    };

    // SB EXTENSIONS

    public static void Print
        (this StringBuilder sb)
        => Console.WriteLine(sb.ToString());

    public static StringBuilder Style
        (this StringBuilder sb)
        => sb.Append("\e[0m");

    public static StringBuilder Color
        (this StringBuilder sb, int cc)
        => sb.Append("\e[").Append(cc)         .Append('m');

    public static StringBuilder Color
        (this StringBuilder sb, ConsoleColor cc)
        => sb.Append("\e[").Append(cc.ToANSI()).Append('m');
}

public class PooledStringBuilder : IDisposable
{
    private static readonly ObjectPool<StringBuilder> Pool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool();

    public readonly StringBuilder sb = Pool.Get();

    public void Dispose()
    {
        sb.Clear();
        Pool.Return(sb);
    }
}

public enum LogLevel
{
    Debug = 0,
    Info  = 1,
    Warn  = 2,
    Error = 3,
}
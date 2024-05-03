using System.Diagnostics.CodeAnalysis;

namespace MemeIndex_Core.Utils;

public static class Logger
{
    public static event Action<string?>? StatusChanged;

    public static void Log(string message) => Console.WriteLine(message);

    public static void Log([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg)
    {
        Console.WriteLine(format, arg);
    }

    public static void Log(ConsoleColor color, string message) => Log(message, color);
    public static void Log(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Log(message);
        Console.ResetColor();
    }

    public static void Log(ConsoleColor color, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg)
    {
        Console.ForegroundColor = color;
        Log(format, arg);
        Console.ResetColor();
    }

    public static void LogError(string location, Exception e)
    {
        Log($"{location}: {e.Message}", ConsoleColor.Red);
        Log("Details: ");
        Log(e.ToString());
    }

    public static bool StatusIsAvailable => StatusChanged != null;

    public static void Status(string? message)
    {
        StatusChanged?.Invoke(message);
    }
}
using System.Diagnostics.CodeAnalysis;

namespace MemeIndex_Core.Utils;

public class Logger
{
    public static void Log(string message) => Console.WriteLine(message);

    public static void Log([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg)
    {
        Console.WriteLine(format, arg);
    }

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

    public static void LogError(string message) => Log(message, ConsoleColor.Red);
}
using System.Diagnostics;

namespace MemeIndex_Core.Utils;

public static class Helpers
{
    // STRING

    public static string Quote(this string text) => $"\"{text}\"";

    public static int FancyHashCode(this object x) => Math.Abs(x.GetHashCode());


    // MATH

    public static int RoundToInt(this double x) => (int)Math.Round(x);

    public static int FloorToEven(this int x) => x >> 1 << 1;


    // CODE WRAPPERS

    /// Executes an action based on a condition value.
    public static void Execute(this bool condition, Action onTrue, Action onFalse)
    {
        (condition ? onTrue : onFalse)();
    }

    /// Just a wrapper for a ternary operator (useful with delegates). 
    public static T Switch<T>(this bool condition, T onTrue, T onFalse)
    {
        return condition ? onTrue : onFalse;
    }

    public static void ForEachTry<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            try
            {
                action(item);
            }
            catch
            {
                /* xd */
            }
        }
    }


    //

    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream stream)
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        await stream.DisposeAsync();
        return memoryStream;
    }


    // STOPWATCH

    public static Stopwatch GetStartedStopwatch()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        return stopwatch;
    }

    public static void Log(this Stopwatch stopwatch, string message)
    {
        Logger.Log($"{stopwatch.Elapsed.TotalSeconds:##0.00000}\t{message}");
        stopwatch.Restart();
    }
}
using System.Diagnostics;

namespace MemeIndex_Core.Utils;

public static class Helpers
{
    public static string Quote(this string text) => $"\"{text}\"";

    public static int FancyHashCode(this object x) => Math.Abs(x.GetHashCode());

    public static bool      FileExists(this string path) =>      File.Exists(path);
    public static bool DirectoryExists(this string path) => Directory.Exists(path);

    public static bool IsImage(this FileInfo file)
    {
        return file.Extension.IsImageFileExtension();
    }

    public static bool IsImageFileExtension(this string extension)
    {
        return GetImageExtensions().Contains(extension.ToLower());
    }

    public static IList<string> GetImageExtensions()
    {
        return new List<string> { ".png", ".jpg", ".jpeg", ".tif", ".bmp" };
    }

    public static IEnumerable<Data.Entities.Directory> GetExisting(this IEnumerable<Data.Entities.Directory> directories)
    {
        return directories.Where(x => x.Path.DirectoryExists());
    }

    public static List<FileInfo> GetImageFiles(string path, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var directory = new DirectoryInfo(path);
        return GetImageExtensions().SelectMany(x => directory.GetFiles($"*{x}", searchOption)).ToList();
    }

    public static FileInfo? GetFileInfo(string path)
    {
        try
        {
            return new FileInfo(path);
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(GetFileInfo), e);
            return null;
        }
    }

    /// <summary>
    /// Executes an action based on a condition value.
    /// </summary>
    public static void Execute(this bool condition, Action onTrue, Action onFalse)
    {
        (condition ? onTrue : onFalse)();
    }

    /// <summary>
    /// Just a wrapper for a ternary operator (useful with delegates). 
    /// </summary>
    public static T Switch<T>(this bool condition, T onTrue, T onFalse)
    {
        return condition ? onTrue : onFalse;
    }

    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream stream)
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        await stream.DisposeAsync();
        return memoryStream;
    }

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

    public static int RoundToInt(this double x) => (int)Math.Round(x);

    public static int FloorToEven(this int x) => x >> 1 << 1;
}
using System.Text.RegularExpressions;

namespace MemeIndex_Core.Utils;

public static partial class Helpers
{
    [GeneratedRegex("[\\s\\n\\r]+")]
    private static partial Regex LineBreaksRegex();

    public static string RemoveLineBreaks(this string text) => LineBreaksRegex().Replace(text, " ");

    public static string Quote(this string text) => $"\"{text}\"";

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

    public static IEnumerable<Entities.Directory> GetExisting(this IEnumerable<Entities.Directory> directories)
    {
        return directories.Where(x => Directory.Exists(x.Path));
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
        catch
        {
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
}
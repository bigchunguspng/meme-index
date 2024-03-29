using System.Text.RegularExpressions;

namespace MemeIndex_Core.Utils;

public static partial class Helpers
{
    [GeneratedRegex("[\\s\\n\\r]+")]
    private static partial Regex LineBreaksRegex();

    public static string RemoveLineBreaks(this string text) => LineBreaksRegex().Replace(text, " ");

    public static bool IsFile     (this string path) =>      File.Exists(path);
    public static bool IsDirectory(this string path) => Directory.Exists(path);

    public static bool IsImage(this FileInfo file)
    {
        return GetImageExtensions().Contains(file.Extension.ToLower());
    }

    public static IList<string> GetImageExtensions()
    {
        return new List<string> { ".png", ".jpg", ".jpeg", ".tif", ".bmp" };
    }

    public static IEnumerable<Entities.Directory> GetExisting(this IEnumerable<Entities.Directory> directories)
    {
        return directories.Where(x => Directory.Exists(x.Path));
    }
}
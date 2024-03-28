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
        var extension = file.Extension.ToLower();
        return extension is ".png" or ".jpg" or ".jpeg" or ".tif" or "bmp";
    }
}
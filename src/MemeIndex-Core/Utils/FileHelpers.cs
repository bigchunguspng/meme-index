using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using Directory = MemeIndex_Core.Data.Entities.Directory;

namespace MemeIndex_Core.Utils;

public static class FileHelpers
{
    public static bool      FileExists(this string path) => System.IO.     File.Exists(path);
    public static bool DirectoryExists(this string path) => System.IO.Directory.Exists(path);

    public static bool IsImage(this FileInfo file)
    {
        return file.Extension.IsImageFileExtension();
    }

    public static bool IsImageFileExtension(this string extension)
    {
        return GetImageExtensions().Contains(extension.ToLower());
    }

    private static List<string>? _imageExtensions;

    public static List<string> GetImageExtensions()
    {
        return _imageExtensions ??= [".png", ".jpg", ".jpeg", ".tif", ".bmp"];
    }

    public static IEnumerable<Directory> GetExisting(this IEnumerable<Directory> directories)
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

    public static async Task<ImageInfo> GetImageInfo(string path)
    {
        try
        {
            return await Image.IdentifyAsync(path);
        }
        catch (Exception e)
        {
            Logger.LogError($"[{nameof(GetImageInfo)}][{path}]", e);
            return new ImageInfo(new PixelTypeInfo(24), new Size(720, 720), null);
        }
    }
}
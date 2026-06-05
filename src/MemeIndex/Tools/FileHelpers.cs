using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace MemeIndex.Tools;

[Obsolete("Unused! Otherwise remove this attribute.")]
public static class FileHelpers
{
    public static FileInfo? GetFileInfo(string path)
    {
        try
        {
            return new FileInfo(path);
        }
        catch (Exception e)
        {
            LogError(e);
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
            LogError($"Can't get image info: {path}. {e}");
            return new ImageInfo(new PixelTypeInfo(24), new Size(720, 720), null);
        }
    }
}
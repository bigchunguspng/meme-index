using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace MemeIndex_Core.Utils;

public static class ImageHelpers
{
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
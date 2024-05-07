using Gdk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using SixLabors.ImageSharp;
using Task = System.Threading.Tasks.Task;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileViewUtils
{
    private LimitedCache<Pixbuf> IconCache { get; } = new(1024);

    public async Task<Pixbuf?> GetImageIcon(string path, int sizeLimit)
    {
        try
        {
            var value = IconCache.TryGetValue(path);
            if (value is not null) return value;

            var info = await Image.IdentifyAsync(path);
            await using var stream = File.OpenRead(path);

            var wide = info.Width > info.Height;

            var aspectRatio = info.Width / (double)info.Height;
            var w = wide ? sizeLimit : sizeLimit * aspectRatio;
            var h = wide ? sizeLimit / aspectRatio : sizeLimit;

            var icon = await LoadPixbufAsync(stream, (int)w, (int)h);
            if (icon is not null)
            {
                IconCache.Add(path, icon);
            }

            return icon;
        }
        catch (Exception e)
        {
            Logger.LogError($"[{nameof(GetImageIcon)}][{path}]", e);
            return null;
        }
    }

    private static Task<Pixbuf?> LoadPixbufAsync(Stream stream, int width, int height) => Task.Run(() =>
    {
        try
        {
            return new Pixbuf(stream, width, height);
        }
        catch
        {
            return null;
        }
    });
}
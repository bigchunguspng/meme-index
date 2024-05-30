using Gdk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using Size = SixLabors.ImageSharp.Size;
using Task = System.Threading.Tasks.Task;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileViewUtils
{
    private LimitedCache<Pixbuf> IconCache { get; } = new(1024);

    public async Task<Pixbuf?> GetImageIcon(string path, int sizeLimit)
    {
        try
        {
            var cached = IconCache.TryGetValue(path);
            if (cached is not null) return cached;

            var info = await Image.IdentifyAsync(path);
            await using var stream = File.OpenRead(path);

            var wide = info.Width > info.Height;
            var aspectRatio = info.Width / (double)info.Height;
            var w = wide ? sizeLimit : sizeLimit * aspectRatio;
            var h = wide ? sizeLimit / aspectRatio : sizeLimit;

            var icon = await LoadPixbufAsync(path, (int)w, (int)h);
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

    private static Task<Pixbuf?> LoadPixbufAsync(string path, int width, int height) => Task.Run(() =>
    {
        try
        {
            var loader = path.EndsWith(".webp").Switch(LoadPixbufWebp, LoadPixbufGeneral);
            return loader.Invoke(path, width, height);
        }
        catch
        {
            return null;
        }
    });

    private static Pixbuf LoadPixbufGeneral(string path, int width, int height)
    {
        using var stream = File.OpenRead(path);
        return new Pixbuf(stream, width, height);
    }

    private static Pixbuf LoadPixbufWebp(string path, int width, int height)
    {
        var options = new DecoderOptions { TargetSize = new Size(width, height) };
        using var image = Image.Load(options, path);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return new Pixbuf(stream.ToArray());
    }
}
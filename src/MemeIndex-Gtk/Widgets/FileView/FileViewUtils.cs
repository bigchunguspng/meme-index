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
            if (path.EndsWith(".webp"))
            {
                var options = new DecoderOptions { TargetSize = new Size(width, height) };
                using var image = Image.Load(options, path);
                using var stream = new MemoryStream();
                image.SaveAsPng(stream);
                return new Pixbuf(stream.ToArray());
            }
            else
            {
                using var stream = File.OpenRead(path);
                return new Pixbuf(stream, width, height);
            }
        }
        catch
        {
            return null;
        }
    });
}
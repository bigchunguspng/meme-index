using Gdk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileViewUtils
{
    private LimitedCache<Pixbuf> IconCache { get; } = new(1024);

    public Pixbuf? GetImageIcon(string path, int w, int h)
    {
        try
        {
            var value = IconCache?.TryGetValue(path);
            if (value is not null) return value;

            using var stream = File.OpenRead(path);
            var icon = new Pixbuf(stream, w, h);

            IconCache?.Add(path, icon);
            return icon;
        }
        catch (Exception e)
        {
            Logger.LogError($"[{nameof(GetImageIcon)}][{path}]", e);
            return null;
        }
    }
}
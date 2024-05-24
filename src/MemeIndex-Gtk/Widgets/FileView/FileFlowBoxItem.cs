using Gdk;
using Gtk;
using Pango;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileFlowBoxItem : Box
{
    private readonly Image _icon;

    public File File { get; }

    public FileFlowBoxItem(File file) : base(Orientation.Vertical, 5)
    {
        File = file;

        _icon = new Image { HeightRequest = 96, WidthRequest = 96 };

        var name = file.Name;
        var text = name.Length > 17 ? name[..14] + '…' : name;
        Add(_icon);
        Add(new Label(text));

        Margin = 5;
        Valign = Align.Start;
    }

    public bool HasIcon => _icon.Pixbuf is not null;

    public void SetIcon(Pixbuf pixbuf)
    {
        _icon.Pixbuf = pixbuf;
        Show();
    }
}
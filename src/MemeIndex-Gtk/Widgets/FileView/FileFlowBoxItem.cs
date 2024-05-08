using Gdk;
using Gtk;
using Pango;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileFlowBoxItem : Box
{
    private readonly Image _icon;

    public File File { get; }

    public FileFlowBoxItem(File file) : base(Orientation.Vertical, 5)
    {
        File = file;

        _icon = new Image { HeightRequest = 96, WidthRequest = 96 };

        Add(_icon);
        Add(new Label(file.Name)
        {
            Justify = Justification.Center,
            MaxWidthChars = 15,
            SingleLineMode = true,
            Ellipsize = EllipsizeMode.End
        });

        Margin = 5;

        Show();
    }

    public bool HasIcon => _icon.Pixbuf is not null;

    public void SetIcon(Pixbuf pixbuf)
    {
        _icon.Pixbuf = pixbuf;
        Show();
    }
}
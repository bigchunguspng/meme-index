using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk;

public class ManageFoldersDialog : Dialog
{
    [UI] private readonly Box _folders = default!;

    [UI] private readonly Button _buttonOk = default!;
    [UI] private readonly Button _buttonCancel = default!;

    private readonly ListBox _listBox;

    private App App { get; init; } = default!;

    public ManageFoldersDialog(MainWindow parent) : this(new Builder("meme-index.glade"))
    {
        Parent = parent;
        App = parent.App;

        var directories = App.Controller.GetTrackedDirectories();
        foreach (var directory in directories)
        {
            if (System.IO.Path.Exists(directory.Path))
            {
                _listBox.Add(new FolderSelectionWidget(_listBox, directory.Path));
            }
        }

        _listBox.Add(new FolderSelectionWidget(_listBox));

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private ManageFoldersDialog(Builder builder) : base(builder.GetRawOwnedObject("ManageFoldersDialog"))
    {
        builder.Autoconnect(this);

        Title = "Manage folders";
        WidthRequest = 360;

        _listBox = new ListBox();
        _listBox.Expand = true;
        _listBox.SelectionMode = SelectionMode.None;
        _folders.Add(_listBox);
    }

    private void Ok(object? sender, EventArgs e)
    {
        // todo save changes
        // rem removed, add added tracked folders
        Hide();
    }

    private void Cancel(object? sender, EventArgs e)
    {
        Hide();
    }
}
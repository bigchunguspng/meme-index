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
            AddFileChooserButton(directory.Path);
        }

        AddFileChooserButton();

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private ManageFoldersDialog(Builder builder) : base(builder.GetRawOwnedObject("ManageFoldersDialog"))
    {
        builder.Autoconnect(this);

        Title = "Manage folders";

        _listBox = new ListBox();
        _listBox.Expand = true;
        _listBox.SelectionMode = SelectionMode.None;
        _folders.Add(_listBox);
    }

    // Load tracked folders
    // create entry for each
    // add empty entry for a new folder
    // add empty entry on every selection
    // rem entry on "-"
    // OK -> rem removed, add added tracked folders

    private void Ok(object? sender, EventArgs e)
    {
        // save changes
        Hide();
    }

    private void Cancel(object? sender, EventArgs e)
    {
        Hide();
    }

    private void AddFileChooserButton(string? path = null)
    {
        var box = new Box(Orientation.Horizontal, 5);
        var button = new Button { Label = "-" };
        var chooser = new FileChooserButton(string.Empty, FileChooserAction.SelectFolder);
        chooser.Expand = true;

        if (path != null && System.IO.Path.Exists(path))
        {
            chooser.SetCurrentFolder(path);
        }

        box.Add(chooser);
        box.Add(button);
        _listBox.Add(box);
    }
}
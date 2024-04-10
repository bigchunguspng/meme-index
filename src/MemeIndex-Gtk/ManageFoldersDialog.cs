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

        var directories = App.IndexingController.GetTrackedDirectories();
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

    private async void Ok(object? sender, EventArgs e)
    {
        Hide();
        await Task.Run(SaveChangesAsync);
    }

    private void Cancel(object? sender, EventArgs e)
    {
        Hide();
    }

    private async void SaveChangesAsync()
    {
        App.SetStatus("Updating watching list...");
        var directoriesDb = App.IndexingController.GetTrackedDirectories().Select(x => x.Path).ToList();
        var directoriesMf = _listBox.Children
            .Select(x => (FolderSelectionWidget)((ListBoxRow)x).Child)
            .Where(x => x.DirectorySelected)
            .Select(x => x.Choice!)
            .ToList();

        var removeList = directoriesDb.Except(directoriesMf).ToList();
        var updateList = directoriesMf.Except(directoriesDb).ToList();

        App.SetStatus($"Removing {removeList.Count} directories...");
        foreach (var directory in removeList)
        {
            await App.IndexingController.RemoveDirectory(directory);
        }

        App.SetStatus($"Adding {updateList.Count} directories...");
        foreach (var directory in updateList)
        {
            await App.IndexingController.AddDirectory(directory);
        }

        App.SetStatus("Watching list updated.");
        await Task.Delay(4000);
        App.SetStatus();
    }
}
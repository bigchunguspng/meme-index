using Gtk;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Widgets;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk.Windows;

public class ManageFoldersDialog : Dialog
{
    [UI] private readonly ListBox _folders = default!;

    [UI] private readonly Button _buttonOk = default!;
    [UI] private readonly Button _buttonCancel = default!;

    private App App { get; }

    public ManageFoldersDialog(MainWindow parent, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        Parent = parent;
        App = parent.App;

        LoadData();

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private void LoadData()
    {
        var directories = App.IndexingService.GetTrackedDirectories();
        foreach (var directory in directories)
        {
            if (System.IO.Path.Exists(directory.Path))
            {
                _folders.Add(new FolderSelectorWidget(_folders, directory.Path));
            }
        }

        _folders.Add(new FolderSelectorWidget(_folders));
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
        var directoriesDb = App.IndexingService.GetTrackedDirectories().Select(x => x.Path).ToList();
        var directoriesMf = _folders.Children
            .Select(x => (FolderSelectorWidget)((ListBoxRow)x).Child)
            .Where(x => x.DirectorySelected)
            .Select(x => x.Choice!)
            .ToList();

        var removeList = directoriesDb.Except(directoriesMf).ToList();
        var updateList = directoriesMf.Except(directoriesDb).ToList();

        foreach (var directory in removeList)
        {
            await App.IndexingService.RemoveDirectory(directory);
        }

        foreach (var directory in updateList)
        {
            await App.IndexingService.AddDirectory(directory);
        }

        App.SetStatus("Watching list updated.");
        App.ClearStatusLater();

        // todo trigger color / ocr indexing process start
    }
}
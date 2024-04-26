using Gtk;
using MemeIndex_Core.Model;
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

    public ManageFoldersDialog(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        App = app;
        Modal = true;

        LoadData();

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private void LoadData()
    {
        var directories = App.IndexController.GetMonitoringOptions().Result;
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
        var monitored = await App.IndexController.GetMonitoringOptions();
        var directoriesDb = monitored.Select(x => x.Path).ToList();
        var directoriesMf = _folders.Children
            .Select(x => (FolderSelectorWidget)((ListBoxRow)x).Child)
            .Where(x => x.DirectorySelected)
            .Select(x => x.Choice!)
            /*.Select(x => new MonitoredDirectory
            {
                Recursive = true,
                Directory = new MemeIndex_Core.Entities.Directory { Path = x.Choice! },
                IndexingOptions = new List<IndexingOption> { new() { MeanId = 1 }, new() { MeanId = 2 } }
            })*/
            .ToList();

        var removeList = directoriesDb.Except(directoriesMf).OrderByDescending(x => x.Length).ToList();
        var updateList = directoriesMf.Except(directoriesDb).OrderBy          (x => x.Length).ToList();

        foreach (var directory in removeList)
        {
            await App.IndexController.RemoveDirectory(directory);
        }

        foreach (var directory in updateList)
        {
            var options = new MonitoringOptions(directory, true, MonitoringOptions.DefaultMeans);
            await App.IndexController.AddDirectory(options);
        }

        App.SetStatus("Watching list updated.");
        App.ClearStatusLater();

        // todo trigger color / ocr indexing process start
    }
}
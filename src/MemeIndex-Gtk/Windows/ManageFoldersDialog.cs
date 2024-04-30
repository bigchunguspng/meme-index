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
                _folders.Add(new FolderSelectorWidget(_folders, directory));
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
        var optionsDb = (await App.IndexController.GetMonitoringOptions()).ToList();
        var optionsMf = _folders.Children
            .Select(x => (FolderSelectorWidget)((ListBoxRow)x).Child)
            .Where(x => x.DirectorySelected)
            .Select(x => new MonitoringOptions
            (
                Path: x.Choice!,
                Recursive: x.Recursive.Active,
                Means: new MonitoringOptions.MeansBuilder()
                    .WithEng(x.Eng.Active)
                    .WithRgb(x.RGB.Active).Build()
            ))
            .ToList();

        var directoriesDb = optionsDb.Select(x => x.Path).ToList();
        var directoriesMf = optionsMf.Select(x => x.Path).ToList();

        var add = directoriesMf.Except(directoriesDb).OrderBy          (x => x.Length).ToList();
        var rem = directoriesDb.Except(directoriesMf).OrderByDescending(x => x.Length).ToList();
        var upd = directoriesDb.Intersect(directoriesMf).Where(path =>
        {
            var db = optionsDb.First(op => op.Path == path);
            var mf = optionsMf.First(op => op.Path == path);
            return !db.IsTheSameAs(mf);
        }).ToList();

        foreach (var directory in rem)
        {
            await App.IndexController.RemoveDirectory(directory);
        }

        foreach (var directory in add)
        {
            var options = optionsMf.First(x => x.Path == directory);
            await App.IndexController.AddDirectory(options);
        }

        foreach (var directory in upd)
        {
            var options = optionsMf.First(x => x.Path == directory);
            await App.IndexController.UpdateDirectory(options);
        }

        App.SetStatus("Watching list updated.");
        App.ClearStatusLater();
    }
}
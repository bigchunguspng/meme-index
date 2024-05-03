using Gtk;
using MemeIndex_Core.Model;
using MemeIndex_Core.Utils;
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
        var existing = directories.Where(x => x.Path.DirectoryExists());
        foreach (var directory in existing)
        {
            _folders.Add(new FolderSelectorWidget(_folders, directory));
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
        Logger.Status("Updating watching list...");

        var options = _folders.Children
            .Select(x => (FolderSelectorWidget)((ListBoxRow)x).Child)
            .Where(x => x.DirectorySelected)
            .Select(x => new MonitoringOptions
            (
                Path: x.Choice!,
                Recursive: x.Recursive.Active,
                Means: new MonitoringOptions.MeansBuilder()
                    .WithEng(x.Eng.Active)
                    .WithRgb(x.RGB.Active).Build()
            ));

        await App.IndexController.UpdateMonitoringDirectories(options);

        Logger.Status("Watching list updated.");

        await App.IndexController.UpdateFileSystemKnowledge();
    }
}
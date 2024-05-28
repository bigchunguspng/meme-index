using Gtk;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Objects;
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

    public readonly Dictionary<int,Mean> Means;

    public ManageFoldersDialog(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        App = app;
        Modal = true;

        Means = App.Context.Means.ToDictionary(x => x.Id);

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
            _folders.Add(new FolderSelectorWidget(this, _folders, directory));
        }

        _folders.Add(new FolderSelectorWidget(this, _folders));
    }

    private async void Ok(object? sender, EventArgs e)
    {
        Hide();
        await Task.Run(SaveChangesAsync);
        Destroy();
    }

    private void Cancel(object? sender, EventArgs e)
    {
        Destroy();
    }

    private async void SaveChangesAsync()
    {
        Logger.Status("Updating watching list...");

        var options = GetSelectedOptions();

        await App.IndexController.UpdateMonitoringDirectories(options);

        Logger.Status("Watching list updated.");

        await App.IndexController.UpdateFileSystemKnowledgeSafe();
    }

    private IEnumerable<MonitoringOption> GetSelectedOptions()
    {
        return GetSelectors()
            .Where(x => x.DirectorySelected)
            .Select(x => x.ExportMonitoringOption());
    }

    private IEnumerable<FolderSelectorWidget> GetSelectors()
    {
        return _folders.Children.Select(x => (FolderSelectorWidget)((ListBoxRow)x).Child);
    }
}
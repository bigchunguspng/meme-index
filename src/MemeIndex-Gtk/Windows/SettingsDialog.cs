using Gtk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using Microsoft.EntityFrameworkCore;
using EventArgs = System.EventArgs;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk.Windows;

public class SettingsDialog : Dialog
{
    [UI] private readonly Box _box = default!;
    [UI] private readonly Entry _entryApiKey = default!;

    [UI] private readonly Button _buttonOk = default!;
    [UI] private readonly Button _buttonCancel = default!;

    private App App { get; }

    public SettingsDialog(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Autoconnect(this);

        App = app;
        Modal = true;

        LoadData();

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private void LoadData()
    {
        _entryApiKey.Text = App.ConfigProvider.GetConfig().OrcApiKey;

        var means = App.Context.Means.AsNoTracking().ToArray();
        foreach (var mean in means)
        {
            var button = new Button { Label = $"Delete all \"{mean.Title}\" search tags" };
            _box.Add(button);
            button.Clicked += async (_, _) =>
            {
                var tagsRemoved = await App.IndexController.RemoveTagsByMean(mean.Id);
                Logger.Status($"Removed {tagsRemoved} \"{mean.Title}\" search tags");
            };
        }
        ShowAll();
    }

    private async void Ok(object? sender, EventArgs e)
    {
        Hide();
        await Task.Run(SaveChanges);
        Destroy();
    }

    private void Cancel(object? sender, EventArgs e)
    {
        Destroy();
    }

    private void SaveChanges()
    {
        App.ConfigProvider.GetConfig().OrcApiKey = _entryApiKey.Text;
        App.ConfigProvider.SaveChanges();
        Logger.Status("Changes saved!");
    }
}
using Gtk;
using MemeIndex_Core;
using MemeIndex_Gtk.Utils;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk.Windows;

public class SettingsDialog : Dialog
{
    [UI] private readonly Entry _entryApiKey = default!;

    [UI] private readonly Button _buttonOk = default!;
    [UI] private readonly Button _buttonCancel = default!;

    private App App { get; }

    public SettingsDialog(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Autoconnect(this);

        App = app;
        Modal = true;

        _entryApiKey.Text = ConfigRepository.GetConfig().OrcApiKey;

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private async void Ok(object? sender, EventArgs e)
    {
        Hide();
        await Task.Run(SaveChanges);
    }

    private void Cancel(object? sender, EventArgs e)
    {
        Hide();
    }

    private void SaveChanges()
    {
        ConfigRepository.GetConfig().OrcApiKey = _entryApiKey.Text;
        ConfigRepository.SaveChanges();
        App.SetStatus("Changes saved!");
        App.ClearStatusLater();
    }
}
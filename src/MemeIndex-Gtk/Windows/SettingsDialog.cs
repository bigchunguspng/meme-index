using Gtk;
using MemeIndex_Core;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk.Windows;

public class SettingsDialog : Dialog
{
    [UI] private readonly Entry _entryApiKey = default!;

    [UI] private readonly Button _buttonOk = default!;
    [UI] private readonly Button _buttonCancel = default!;

    private App App { get; init; } = default!;

    public SettingsDialog(MainWindow parent) : this(new Builder("SettingsDialog.glade"))
    {
        Parent = parent;
        App = parent.App;

        _entryApiKey.Text = ConfigRepository.GetConfig().OrcApiKey;

        ShowAll();

        _buttonOk.Clicked += Ok;
        _buttonCancel.Clicked += Cancel;
    }

    private SettingsDialog(Builder builder) : base(builder.GetRawOwnedObject("SettingsDialog"))
    {
        builder.Autoconnect(this);

        Title = "Settings";
        WidthRequest = 360;
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
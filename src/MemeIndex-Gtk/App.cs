using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Services;

namespace MemeIndex_Gtk;

public class App
{
    public IndexingController Controller { get; init; }
    public IOcrService OcrService { get; init; }
    public ColorTagService ColorTagService { get; init; }

    public App(IndexingController controller, IOcrService ocrService, ColorTagService colorTagService)
    {
        Controller = controller;
        OcrService = ocrService;
        ColorTagService = colorTagService;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        Application.Init();

        var app = new Application("org.MemeIndex_Gtk.MemeIndex_Gtk", GLib.ApplicationFlags.None);
        app.Register(GLib.Cancellable.Current);

        var win = new MainWindow(this);
        app.AddWindow(win);

        Controller.StartIndexing(); // move into app ?

        win.Show();
        Application.Run();

        return Task.CompletedTask;
    }

    /*
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Controller.StopIndexing();
        return Task.CompletedTask;
    }*/
}
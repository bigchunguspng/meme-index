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

    public void Start()
    {
        Application.Init();

        var app = new Application("org.MemeIndex_Gtk.MemeIndex_Gtk", GLib.ApplicationFlags.None);
        var win = new MainWindow(this);

        app.Register(GLib.Cancellable.Current);
        app.AddWindow(win);

        Controller.StartIndexing();

        win.Show();
        Application.Run();
    }

    public void Stop()
    {
        Controller.StopIndexing();
    }
}
using GLib;
using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Services;
using Application = Gtk.Application;

namespace MemeIndex_Gtk;

public class App
{
    private Statusbar? _status;

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

        var app = new Application("org.MemeIndex_Gtk.MemeIndex_Gtk", ApplicationFlags.None);
        var win = new MainWindow(this);

        app.Register(Cancellable.Current);
        app.AddWindow(win);

        Controller.StartIndexing();

        win.Show();
        Application.Run();
    }

    public void Stop()
    {
        Controller.StopIndexing();
    }

    public void SetStatusBar(Statusbar bar) => _status = bar;

    public void SetStatus(string? message = null) => Application.Invoke((_, _) =>
    {
        _status?.Pop(0);
        if (message != null)
        {
            _status?.Push(0, message);
        }
    });
}
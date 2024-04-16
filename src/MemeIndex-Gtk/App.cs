using GLib;
using Gtk;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Services.Search;
using MemeIndex_Gtk.Windows;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace MemeIndex_Gtk;

public class App
{
    private Statusbar? _status;

    public SearchService SearchService { get; init; }
    public IndexingService IndexingService { get; init; }
    public IOcrService OcrService { get; init; }
    public ColorTagService ColorTagService { get; init; }
    public MemeDbContext Context { get; init; }

    public App
    (
        IndexingService indexingService,
        SearchService searchService,
        IOcrService ocrService,
        ColorTagService colorTagService,
        MemeDbContext context
    )
    {
        IndexingService = indexingService;
        SearchService = searchService;
        OcrService = ocrService;
        ColorTagService = colorTagService;
        Context = context;

        IndexingService.Log += SetStatus;
    }

    public void Start()
    {
        Application.Init();

        var app = new Application("org.MemeIndex_Gtk.MemeIndex_Gtk", ApplicationFlags.None);
        var win = new MainWindow(this);

        app.Register(Cancellable.Current);
        app.AddWindow(win);
        win.Show();

        Task.Run(() =>
        {
            SetStatus("Loading database...");
            DatabaseInitializer.EnsureCreated(Context);
            IndexingService.StartIndexingAsync();
        });

        Application.Run();
    }

    public void Stop()
    {
        IndexingService.StopIndexing();
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

    public async void ClearStatusLater()
    {
        await Task.Delay(4000);
        SetStatus();
    }
}
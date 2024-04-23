using GLib;
using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Services.OCR;
using MemeIndex_Core.Services.Search;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Windows;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace MemeIndex_Gtk;

public class App : IDisposable
{
    private Statusbar? _status;
    private readonly CustomCss _css;

    public SearchService SearchService { get; }
    public IndexingService IndexingService { get; }
    public IndexController IndexController { get; }
    public IOcrService OcrService { get; }
    public ColorTagService ColorTagService { get; }
    public MemeDbContext Context { get; }

    public App
    (
        IndexingService indexingService,
        IndexController indexController,
        SearchService searchService,
        IOcrService ocrService,
        ColorTagService colorTagService,
        MemeDbContext context,
        CustomCss css
    )
    {
        _css = css;

        IndexingService = indexingService;
        IndexController = indexController;
        SearchService = searchService;
        OcrService = ocrService;
        ColorTagService = colorTagService;
        Context = context;

        Logger.StatusChange += SetStatus;
    }

    public void Start()
    {
        Application.Init();

        var app = new Application("only.in.ohio.MemeIndex", ApplicationFlags.None);

        _css.AddProviders();

        var win = new MainWindow(this, new WindowBuilder(nameof(MainWindow)));

        app.Register(Cancellable.Current);
        app.AddWindow(win);
        win.Show();

        Task.Run(() =>
        {
            SetStatus("Loading database...");
            DatabaseInitializer.EnsureCreated(Context);
            IndexController.StartIndexing();
        });

        Application.Run();
    }

    public void Dispose()
    {
        IndexController.StopIndexing();
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
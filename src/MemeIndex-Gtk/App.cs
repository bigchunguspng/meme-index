using GLib;
using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services.ImageToText.ColorTag;
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

    public IndexController IndexController { get; }
    public SearchController SearchController { get; }
    public ColorSearchProfile ColorSearchProfile { get; }
    public MemeDbContext Context { get; }

    public App
    (
        IndexController indexController,
        SearchController searchController,
        ColorSearchProfile colorSearchProfile,
        MemeDbContext context,
        CustomCss css
    )
    {
        _css = css;

        IndexController = indexController;
        SearchController = searchController;
        ColorSearchProfile = colorSearchProfile;
        Context = context;

        Logger.StatusChanged += SetStatus;
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
            Context.Access.Take().Wait();
            Context.EnsureCreated();
            IndexController.StartIndexing();
            Context.Access.Release();
        });

        Application.Run();
    }

    public void Dispose()
    {
        IndexController.StopIndexing();
    }

    public void SetStatusBar(Statusbar bar) => _status = bar;

    private void SetStatus(string? message) => Application.Invoke((_, _) =>
    {
        _status?.Pop(0);
        if (message != null)
        {
            _status?.Push(0, message);
        }
    });
}
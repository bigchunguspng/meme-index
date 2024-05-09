using GLib;
using Gtk;
using MemeIndex_Core;
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

    public IConfigProvider<ConfigGtk> ConfigProvider { get; }
    public IndexController IndexController { get; }
    public SearchController SearchController { get; }
    public ColorSearchProfile ColorSearchProfile { get; }
    public MemeDbContext Context { get; }

    public App
    (
        IConfigProvider<ConfigGtk> configProvider,
        IndexController indexController,
        SearchController searchController,
        ColorSearchProfile colorSearchProfile,
        MemeDbContext context,
        CustomCss css
    )
    {
        _css = css;

        ConfigProvider = configProvider;
        IndexController = indexController;
        SearchController = searchController;
        ColorSearchProfile = colorSearchProfile;
        Context = context;

        Logger.StatusChanged += SetStatus;
    }

    public void Start()
    {
        Application.Init(); // 0.13 sec

        var app = new Application("only.in.ohio.MemeIndex", ApplicationFlags.None);

        _css.AddProviders(); // 0.14 sec

        var win = new MainWindow(this, new WindowBuilder(nameof(MainWindow))); // 0.47 sec

        app.Register(Cancellable.Current); // 0.15 sec
        app.AddWindow(win);
        win.Show(); // 0.81 sec

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

    private void SetStatus(string? message) => SetStatus(0, message);
    
    private void SetStatus(int id, string? message) => Application.Invoke((_, _) =>
    {
        var uintId = (uint)Math.Clamp(id + int.MaxValue + 1, uint.MinValue, uint.MaxValue);

        _status?.Pop(uintId);
        if (message != null) _status?.Push(uintId, message);
    });
}
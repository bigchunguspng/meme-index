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

    private readonly Lazy<IndexController> _indexController;
    private readonly Lazy<SearchController> _searchController;
    private readonly Lazy<MemeDbContext> _context;

    public IndexController IndexController => _indexController.Value;
    public SearchController SearchController => _searchController.Value;
    public MemeDbContext Context => _context.Value;
    public IConfigProvider<ConfigGtk> ConfigProvider { get; }
    public ColorSearchProfile ColorSearchProfile { get; }

    public App
    (
        Lazy<IndexController> indexController,
        Lazy<SearchController> searchController,
        Lazy<MemeDbContext> context,
        IConfigProvider<ConfigGtk> configProvider,
        ColorSearchProfile colorSearchProfile,
        CustomCss css
    )
    {
        _css = css;

        _indexController = indexController;
        _searchController = searchController;
        _context = context;

        ConfigProvider = configProvider;
        ColorSearchProfile = colorSearchProfile;

        Logger.StatusChanged += SetStatus;
    }

    public void Start()
    {
        var sw = Helpers.GetStartedStopwatch();
        Application.Init(); // 0.13 sec

        _css.AddProviders(); // 0.14 sec

        var win = new MainWindow(this, new WindowBuilder(nameof(MainWindow))); // 0.20 sec

        sw.Log("mainWindow.Show...");
        win.ShowAll(); // 0.70-0.90 sec
        sw.Log("mainWindow.Show");

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
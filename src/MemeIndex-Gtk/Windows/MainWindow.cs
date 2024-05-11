using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Objects;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Widgets;
using MemeIndex_Gtk.Widgets.FileView;
using Application = Gtk.Application;
using File = MemeIndex_Core.Data.Entities.File;
using MenuItem = Gtk.MenuItem;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace MemeIndex_Gtk.Windows;

public class MainWindow : Window
{
    [UI] private readonly SearchEntry _search = default!;
    [UI] private readonly Statusbar _status = default!;

    [UI] private readonly MenuItem _menuFileQuit = default!;
    [UI] private readonly MenuItem _menuFileFolders = default!;
    [UI] private readonly MenuItem _menuFileSettings = default!;

    [UI] private readonly Box _colorSearch = default!;
    [UI] private readonly ScrolledWindow _scroll = default!;

    [UI] private readonly ToggleButton _buttonColorSearch = default!;
    [UI] private readonly Button _buttonSwitchFileView = default!;

    private readonly ColorSearchPanel _colorSearchPanel;

    private readonly Dictionary<bool, IFileView> _fileViewPool = new();
    private IFileView? _fileView;
    private bool _largeIcons;

    public App App { get; }

    public MainWindow(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        // todo make custom decoration (optional, default for Windows 10™)
        // Decorated = false;

        App = app;
        app.SetStatusBar(_status);

        var position = App.ConfigProvider.GetConfig().WindowPosition; // 0.21 (-> JsonIO) --> 0.00 sec
        if (position.HasValue)
        {
            var p = position.Value;
            Move(p.X, p.Y);
            Resize(p.Width, p.Height);
        }

        _colorSearchPanel = new ColorSearchPanel(app, new WindowBuilder(nameof(ColorSearchPanel))); // 0.08 --> 0.00 sec
        _colorSearch.PackStart(_colorSearchPanel, true, true, 0);

        _largeIcons = App.ConfigProvider.GetConfig().FileViewLargeIcons ?? true;
        _buttonSwitchFileView.Clicked += (_, _) =>
        {
            _largeIcons = !_largeIcons;

            App.ConfigProvider.GetConfig().FileViewLargeIcons = _largeIcons;
            app.ConfigProvider.SaveChanges();

            ChangeFileView();
        };

        ChangeFileView(); // 0.04 sec flow | 0.10 sec tree

        DeleteEvent             += Window_DeleteEvent;
        _menuFileQuit.Activated += Window_DeleteEvent;
        _menuFileFolders .Activated += OpenManageFoldersDialog;
        _menuFileSettings.Activated += OpenSettingsDialog;

        _search.SearchChanged += OnSearchChanged;
        _colorSearchPanel.SelectionChanged += OnColorSelectionChanged;

        var showPanel = App.ConfigProvider.GetConfig().ShowColorSearchPanel;
        _colorSearch.Visible = showPanel ?? false;
        _buttonColorSearch.Active = _colorSearch.Visible;
        _buttonColorSearch.Toggled += ButtonColorSearchOnToggled;
    }

    private void ChangeFileView()
    {
        _buttonSwitchFileView.Label = _largeIcons ? "Table" : "Large Icons";

        if (_scroll.Children.Length > 0)
            _scroll.Remove(_scroll.Children[0]);

        if (_fileViewPool.TryGetValue(_largeIcons, out var value)) _fileView = value;
        else
        {
            _fileView = _largeIcons ? new FileFlowView(App, _scroll) : new FileTreeView(App);
            _fileViewPool[_largeIcons] = _fileView;
        }

        _scroll.Add(_fileView.AsWidget());

        if (_files is not null)
            _fileView.ShowFiles(_files);
    }

    #region SEARCH

    private readonly List<SearchQuery> _queries = new(2);
    private List<File>? _files;

    private void UpdateQuery(int meanId, IEnumerable<string> words)
    {
        var query = _queries.FirstOrDefault(x => x.MeanId == meanId);
        if (query is not null)
        {
            query.Words.Clear();
            query.Words.AddRange(words);
        }
        else
        {
            _queries.Add(new SearchQuery(meanId, words.ToList(), LogicalOperator.AND));
        }
    }

    private async void Search()
    {
        await App.Context.Access.Take();

        _files = App.SearchController.Search(_queries, LogicalOperator.AND).Result.ToList();

        App.Context.Access.Release();

        var searchByMean = _queries.Select(x => $"Mean #{x.MeanId}: [{string.Join(' ', x.Words)}]");
        Logger.Status(GetHashCode(), $"Files: {_files.Count}, search: {string.Join(' ', searchByMean)}");

        await _fileView!.ShowFiles(_files);
    }

    #endregion


    #region EVENT HANDLERS

    private async void OpenManageFoldersDialog(object? sender, EventArgs e)
    {
        await App.Context.Access.Take();
        var builder = new WindowBuilder(nameof(ManageFoldersDialog));
        var window = new ManageFoldersDialog(App, builder);
        App.Context.Access.Release();
        window.Show();
    }

    private void OpenSettingsDialog(object? sender, EventArgs e)
    {
        var builder = new WindowBuilder(nameof(SettingsDialog));
        new SettingsDialog(App, builder).Show();
    }

    private void OnSearchChanged(object? sender, EventArgs e)
    {
        var words = _search.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        UpdateQuery(2, words);
        Search();
    }

    private void OnColorSelectionChanged(object? sender, EventArgs e)
    {
        UpdateQuery(1, _colorSearchPanel.SelectedColors);
        Search();
    }

    private void ButtonColorSearchOnToggled(object? sender, EventArgs e)
    {
        var active = _buttonColorSearch.Active;
        active.Execute(_colorSearch.Show, _colorSearch.Hide);
        App.ConfigProvider.GetConfig().ShowColorSearchPanel = active;
        App.ConfigProvider.SaveChanges();
    }

    private void Window_DeleteEvent(object? sender, EventArgs a)
    {
        GetPosition(out var x, out var y);
        var position = new Rectangle(x, y, Allocation.Width, Allocation.Height);
        App.ConfigProvider.GetConfig().WindowPosition = position;
        App.ConfigProvider.SaveChanges();
        Application.Quit();
    }

    #endregion
}
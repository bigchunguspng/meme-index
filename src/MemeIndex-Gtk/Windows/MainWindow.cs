using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Widgets;
using Application = Gtk.Application;
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

    private readonly ColorSearchPanel _colorSearchPanel;
    private readonly FileView _files;

    public App App { get; }

    public MainWindow(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        // Decorated = false; // todo make custom decoration for windows 

        App = app;
        app.SetStatusBar(_status);

        _colorSearchPanel = new ColorSearchPanel(app, new WindowBuilder(nameof(ColorSearchPanel)));
        _colorSearch.PackStart(_colorSearchPanel, true, true, 0);

        _files = new FileView(App);
        _scroll.Add(_files);

        DeleteEvent             += Window_DeleteEvent;
        _menuFileQuit.Activated += Window_DeleteEvent;
        _menuFileFolders .Activated += OpenManageFoldersDialog;
        _menuFileSettings.Activated += OpenSettingsDialog;

        _search.SearchChanged += OnSearchChanged;
        _colorSearchPanel.SelectionChanged += OnColorSelectionChanged;

        _buttonColorSearch.Toggled += ButtonColorSearchOnToggled;
        _colorSearch.Visible = _buttonColorSearch.Active;
    }


    #region SEARCH

    private readonly List<SearchQuery> _queries = new(2);

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

        var files = App.SearchController.Search(_queries, LogicalOperator.AND).Result.ToList();

        App.Context.Access.Release();

        var txt = _queries.Select(x => $"Mean #{x.MeanId}: [{string.Join(' ', x.Words)}]");
        Logger.Status($"Files: {files.Count}, search: {string.Join(' ', txt)}");

        await _files.ShowFiles(files);
    }

    #endregion


    #region EVENT HANDLERS

    private void OpenManageFoldersDialog(object? sender, EventArgs e)
    {
        var builder = new WindowBuilder(nameof(ManageFoldersDialog));
        new ManageFoldersDialog(App, builder).Show();
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
        _buttonColorSearch.Active.Execute(_colorSearch.Show, _colorSearch.Hide);
    }

    private static void Window_DeleteEvent(object? sender, EventArgs a)
    {
        Application.Quit();
    }

    #endregion
}
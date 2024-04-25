using Gtk;
using MemeIndex_Core.Controllers;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Widgets;
using Pango;
using Application = Gtk.Application;
using MenuItem = Gtk.MenuItem;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace MemeIndex_Gtk.Windows;

public class MainWindow : Window
{
    [UI] private readonly SearchEntry _search = default!;
    [UI] private readonly TreeView _files = default!;
    [UI] private readonly Statusbar _status = default!;

    [UI] private readonly MenuItem _menuFileQuit = default!;
    [UI] private readonly MenuItem _menuFileFolders = default!;
    [UI] private readonly MenuItem _menuFileSettings = default!;

    [UI] private readonly Box _colorSearch = default!;

    [UI] private readonly ToggleButton _buttonColorSearch = default!;

    private readonly ColorSearchPanel _colorSearchPanel;

    public App App { get; }

    public MainWindow(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        // Decorated = false; // todo make custom decoration for windows 

        App = app;
        app.SetStatusBar(_status);

        _colorSearchPanel = new ColorSearchPanel(app, new WindowBuilder(nameof(ColorSearchPanel)));
        _colorSearch.PackStart(_colorSearchPanel, true, true, 0);

        ConstructFilesView();

        DeleteEvent             += Window_DeleteEvent;
        _menuFileQuit.Activated += Window_DeleteEvent;
        _menuFileFolders .Activated += OpenManageFoldersDialog;
        _menuFileSettings.Activated += OpenSettingsDialog;

        _search.SearchChanged += OnSearchChanged;
        _colorSearchPanel.SelectionChanged += OnColorSelectionChanged;

        _buttonColorSearch.Toggled += ButtonColorSearchOnToggled;
        _colorSearch.Visible = _buttonColorSearch.Active;
    }


    #region FILES

    private void ConstructFilesView()
    {
        _files.AppendColumn("Name", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 0);
        _files.AppendColumn("Path", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 1);

        foreach (var column in _files.Columns)
        {
            column.Resizable = true;
            column.Reorderable = true;
            column.FixedWidth = 200;
            column.Expand = true;
        }

        _files.EnableSearch = false;
        _files.ActivateOnSingleClick = true;
        _files.RowActivated += (_, args) =>
        {
            var column = (TreeViewColumn)args.Args[1];
            var renderer = (CellRendererText)column.Cells[0];
            App.SetStatus(renderer.Text);
        };
        _files.FocusOutEvent += (_, _) =>
        {
            _files.Selection.UnselectAll();
            App.SetStatus();
        };
    }

    private static ListStore CreateStore()
    {
        var store = new ListStore(typeof(string), typeof(string));

        //store.DefaultSortFunc = SortFunc;
        store.SetSortColumnId(1, SortType.Ascending);

        return store;
    }

    private static void FillStore(ListStore store, List< MemeIndex_Core.Entities.File> files)
    {
        store.Clear();

        foreach (var file in files)
        {
            store.AppendValues(file.Name, file.Directory.Path);
        }
    }

    /*private static int SortFunc(ITreeModel model, TreeIter a, TreeIter b)
    {
        var aPath = (string)model.GetValue(a, 1);
        var bPath = (string)model.GetValue(b, 1);
        var aName = (string)model.GetValue(a, 0);
        var bName = (string)model.GetValue(b, 0);

        var dirs = string.CompareOrdinal(aPath, bPath);
        return dirs == 0 ? string.CompareOrdinal(aName, bName) : dirs;
    }*/

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
        var files = App.SearchController.Search(_queries, LogicalOperator.AND).Result.ToList();

        var txt = _queries.Select(x => $"Mean #{x.MeanId}: [{string.Join(' ', x.Words)}]");
        App.SetStatus($"Files: {files.Count}, search: {string.Join(' ', txt)}");

        var store = CreateStore();
        await Task.Run(() => FillStore(store, files));
        _files.Model = store;
    }

    #endregion


    #region EVENT HANDLERS

    private void OpenManageFoldersDialog(object? sender, EventArgs e)
    {
        var builder = new WindowBuilder(nameof(ManageFoldersDialog));
        new ManageFoldersDialog(this, builder).Show();
    }

    private void OpenSettingsDialog(object? sender, EventArgs e)
    {
        var builder = new WindowBuilder(nameof(SettingsDialog));
        new SettingsDialog(this, builder).Show();
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
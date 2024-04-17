using Gtk;
using MemeIndex_Gtk.Utils;
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

    [UI] private readonly Grid _gridColorsFunny = default!;
    [UI] private readonly Grid _gridColorsGray = default!;

    [UI] private readonly Frame _colorSearch = default!;

    [UI] private readonly ToggleButton _buttonColorSearch = default!;
    [UI] private readonly Button _buttonClearColorSelection = default!;

    public App App { get; }

    public MainWindow(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        // Decorated = false; // todo make custom decoration for windows 

        App = app;
        app.SetStatusBar(_status);

        ConstructColorSearchPalette();
        ConstructFilesView();

        DeleteEvent             += Window_DeleteEvent;
        _menuFileQuit.Activated += Window_DeleteEvent;
        _menuFileFolders .Activated += OpenManageFoldersDialog;
        _menuFileSettings.Activated += OpenSettingsDialog;

        _search.SearchChanged += OnSearchChangedAsync;

        _buttonClearColorSelection.Clicked += ClearColorSelectionOnClicked;
        _buttonColorSearch.Clicked += ButtonColorSearchOnClicked;
        _colorSearch.Visible = _buttonColorSearch.Active;
    }

    private void ConstructColorSearchPalette()
    {
        int top;
        var left = 0;
        foreach (var hue in App.ColorTagService.ColorsFunny)
        {
            top = 0;
            foreach (var color in hue.Value.Take(6))
            {
                AddSearchableColor(top++, left, color.Key, _gridColorsFunny);
            }

            left++;
        }

        top = 0;
        foreach (var color in App.ColorTagService.ColorsGrayscale.Reverse())
        {
            AddSearchableColor(top++, 0, color.Key, _gridColorsGray);
        }
    }

    private void AddSearchableColor(int top, int left, string key, Grid grid)
    {
        var checkbutton = new CheckButton
        {
            Visible = true,
            FocusOnClick = false
        };
        checkbutton.StyleContext.AddClass("color");
        checkbutton.StyleContext.AddClass(key);
        // todo += checked handler;
        grid.Attach(checkbutton, left, top, 1, 1);
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

    private void FillStore(ListStore store, string search)
    {
        store.Clear();

        var files = App.SearchService.SearchByText(search).Result?.ToList();
        if (files != null)
        {
            App.SetStatus($"Files: {files.Count}, search: {search}.");
            foreach (var file in files)
            {
                store.AppendValues(file.Name, file.Directory.Path);
            }
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

    private async void OnSearchChangedAsync(object? sender, EventArgs e)
    {
        var store = CreateStore();
        await Task.Run(() => FillStore(store, _search.Text));
        _files.Model = store;
    }

    private void ClearColorSelectionOnClicked(object? sender, EventArgs e)
    {
        DeactivateCheckboxes(_gridColorsFunny);
        DeactivateCheckboxes(_gridColorsGray);
    }

    private void ButtonColorSearchOnClicked(object? sender, EventArgs e)
    {
        if (_buttonColorSearch.Active)
            _colorSearch.Show();
        else
            _colorSearch.Hide();
    }

    private static void DeactivateCheckboxes(Container grid)
    {
        var active = grid.Children.OfType<CheckButton>().Where(x => x.Active);
        foreach (var checkButton in active) checkButton.Active = false;
    }

    private static void Window_DeleteEvent(object? sender, EventArgs a)
    {
        Application.Quit();
    }

    #endregion
}
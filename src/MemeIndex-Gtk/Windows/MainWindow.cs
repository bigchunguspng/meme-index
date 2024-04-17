using Gtk;
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

    public App App { get; init; } = default!;

    public MainWindow(App app) : this(new Builder("MainWindow.glade"))
    {
        App = app;
        app.SetStatusBar(_status);

        ConstructColorSearchPalette();
    }

    private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
    {
        builder.Autoconnect(this);

        _files.AppendColumn("Name", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 0);
        _files.AppendColumn("Path", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 1);
        _files.EnableSearch = false;

        foreach (var column in _files.Columns)
        {
            column.Resizable = true;
            column.Reorderable = true;
            column.FixedWidth = 200;
            column.Expand = true;
        }

        DeleteEvent += Window_DeleteEvent;
        _menuFileQuit.Activated += Window_DeleteEvent;
        _menuFileFolders.Activated += ManageFolders;
        _menuFileSettings.Activated += Settings;
        _search.SearchChanged += OnSearchChanged;
        _buttonClearColorSelection.Clicked += ClearColorSelectionOnClicked;
        _colorSearch.Visible = _buttonColorSearch.Active;
        _buttonColorSearch.Clicked += ButtonColorSearchOnClicked;

        // todo move that crap to upper ctor
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

    private void ManageFolders(object? sender, EventArgs e)
    {
        new ManageFoldersDialog(this).Show();
    }
    
    private void Settings(object? sender, EventArgs e)
    {
        new SettingsDialog(this).Show();
    }

    private void OnSearchChanged(object? sender, EventArgs e)
    {
        var store = CreateStore();
        FillStore(store, _search.Text);
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

    private void Window_DeleteEvent(object? sender, EventArgs a)
    {
        Application.Quit();
    }

    ListStore CreateStore()
    {
        var store = new ListStore(typeof(string), typeof(string));

        //store.DefaultSortFunc = SortFunc;
        store.SetSortColumnId(1, SortType.Ascending);

        return store;
    }

    void FillStore(ListStore store, string search)
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

    /*int SortFunc (TreeModel model, TreeIter a, TreeIter b)
    {
        // sorts folders before files
        bool a_is_dir = (bool) model.GetValue (a, COL_IS_DIRECTORY);
        bool b_is_dir = (bool) model.GetValue (b, COL_IS_DIRECTORY);
        string a_name = (string) model.GetValue (a, COL_DISPLAY_NAME);
        string b_name = (string) model.GetValue (b, COL_DISPLAY_NAME);

        if (!a_is_dir && b_is_dir)
            return 1;
        else if (a_is_dir && !b_is_dir)
            return -1;
        else
            return String.Compare (a_name, b_name);
    }*/
}
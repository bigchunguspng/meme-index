using Gtk;
using Pango;
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

    public App App { get; init; } = default!;

    public MainWindow(App app) : this(new Builder("meme-index.glade"))
    {
        App = app;
        app.SetStatusBar(_status);
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
        _search.SearchChanged += OnSearchChanged;
    }

    private void ManageFolders(object? sender, EventArgs e)
    {
        new ManageFoldersDialog(this).Show();
    }

    private void OnSearchChanged(object? sender, EventArgs e)
    {
        var store = CreateStore();
        FillStore(store, _search.Text);
        _files.Model = store;
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
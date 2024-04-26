using Gdk;
using Gtk;
using MemeIndex_Gtk.Utils;
using Pango;

namespace MemeIndex_Gtk.Widgets;

public class FileView : TreeView
{
    private List<MemeIndex_Core.Entities.File>? _files;
    private MemeIndex_Core.Entities.File? _selectedFile;

    private App App { get; }

    public FileView(App app)
    {
        App = app;

        AppendColumn("Name", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 0);
        AppendColumn("Path", new CellRendererText { Ellipsize = EllipsizeMode.End }, "text", 1);

        foreach (var column in Columns)
        {
            column.Resizable = true;
            column.Reorderable = true;
            column.FixedWidth = 200;
            column.Expand = true;
        }

        Visible = true;
        EnableSearch = false;
        ActivateOnSingleClick = true;
        RowActivated += (_, args) =>
        {
            var index = args.Path.Indices[0];
            if (_files is not null && _files.Count > index)
            {
                _selectedFile = _files[index];
                var x = _selectedFile;
                var fullPath = System.IO.Path.Combine(x.Directory.Path, x.Name);
                App.SetStatus($"{fullPath}, {x.Size} bytes, Modified: {x.Modified:F}");
            }
        };
        SelectCursorRow += (sender, _) => OpenFile(sender, EventArgs.Empty);
        ButtonPressEvent += (o, args) =>
        {
            if (args.Event.Type == EventType.DoubleButtonPress) OpenFile(o, EventArgs.Empty);
        };

        PopupMenu += FilesOnPopupMenu;
        ButtonReleaseEvent += FilesOnButtonPressEvent;
    }

    private void FilesOnButtonPressEvent(object o, ButtonReleaseEventArgs args)
    {
        if (args.Event.Button == 3) OpenFilesContextMenu();
    }

    private void FilesOnPopupMenu(object o, PopupMenuArgs args)
    {
        OpenFilesContextMenu();
    }

    private void OpenFilesContextMenu()
    {
        var menu = new Menu();

        var itemO = new MenuItem("Open");
        var itemE = new MenuItem("Show in Explorer");

        var fileSelected = _selectedFile is not null;

        itemO.Sensitive = fileSelected;
        itemE.Sensitive = fileSelected;

        itemO.Activated += OpenFile;
        itemE.Activated += ShowFileInExplorer;

        menu.Add(itemO);
        menu.Add(itemE);

        menu.ShowAll();
        menu.Popup();
    }

    private void OpenFile(object? sender, EventArgs e)
    {
        if (_selectedFile is null) return;

        FileOpener.OpenFileWithDefaultApp(_selectedFile.GetFullPath());
    }

    private void ShowFileInExplorer(object? sender, EventArgs e)
    {
        if (_selectedFile is null) return;

        FileOpener.ShowFileInExplorer(_selectedFile.GetFullPath());
    }

    public async Task ShowFiles(List<MemeIndex_Core.Entities.File> files)
    {
        var store = CreateStore();
        await Task.Run(() => FillStore(store, files));
        Model = store;
    }

    private static ListStore CreateStore()
    {
        var store = new ListStore(typeof(string), typeof(string));

        //store.DefaultSortFunc = SortFunc;
        store.SetSortColumnId(1, SortType.Ascending);

        return store;
    }

    private void FillStore(ListStore store, List<MemeIndex_Core.Entities.File> files)
    {
        store.Clear();

        _selectedFile = null;
        _files = files;

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
}
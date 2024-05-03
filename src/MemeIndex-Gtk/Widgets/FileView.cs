using Gdk;
using Gtk;
using Humanizer;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Utils.FileOpener;
using Pango;
using TextCopy;

namespace MemeIndex_Gtk.Widgets;

public class FileView : TreeView
{
    private List<MemeIndex_Core.Entities.File>? _files;
    private MemeIndex_Core.Entities.File? _selectedFile;

    private readonly LimitedCache<Pixbuf> _iconCache;
    private readonly FileOpener _fileOpener;

    private App App { get; }

    public FileView(App app)
    {
        App = app;

        _iconCache = new LimitedCache<Pixbuf>(1024);
        _fileOpener = FileOpenerFactory.GetFileOpener();

        var col1 = new TreeViewColumn { Title = "Name" };
        var col2 = new TreeViewColumn { Title = "Path" };

        var ren0 = new CellRendererPixbuf();
        var ren1 = new CellRendererText { Ellipsize = EllipsizeMode.End };
        var ren2 = new CellRendererText { Ellipsize = EllipsizeMode.End };

        col1.PackStart(ren0, false);
        col1.PackStart(ren1, true);
        col2.PackStart(ren2, true);

        AppendColumn(col1);
        AppendColumn(col2);

        col1.AddAttribute(ren0, "pixbuf", 0);
        col1.AddAttribute(ren1, "text", 1);
        col2.AddAttribute(ren2, "text", 2);

        foreach (var column in Columns)
        {
            column.Resizable = true;
            column.Reorderable = true;
            column.FixedWidth = 200;
            column.Expand = true;
        }

        Visible = true;
        EnableSearch = false;
        CursorChanged += (_, _) =>
        {
            var rows = Selection.GetSelectedRows();
            var index = rows.Length > 0 ? rows[0].Indices[0] : -1;
            if (index >= 0 && _files is not null && _files.Count > index)
            {
                _selectedFile = _files[index];
                var path = _selectedFile.GetFullPath();
                var size = _selectedFile.Size.Bytes().ToString("#.#");
                var date = _selectedFile.Modified.ToLocalTime().ToString("dd.MM.yyyy' 'HH:mm");
                Logger.Status($"Size: {size}, Modified: {date}, Path: {path}");
            }
            else
            {
                _selectedFile = null;
            }
        };
        RowActivated += (sender, _) => OpenFile(sender, EventArgs.Empty);
        ButtonPressEvent += (o, args) =>
        {
            if (args.Event.Type == EventType.DoubleButtonPress) OpenFile(o, EventArgs.Empty);
        };
        PopupMenu += (_, _) => OpenFilesContextMenu();
        ButtonReleaseEvent += (_, args) =>
        {
            if (args.Event.Button == 3) OpenFilesContextMenu();
        };
    }

    // ACTIONS

    private void OpenFilesContextMenu()
    {
        var menu = new Menu();

        var itemO = new MenuItem("Open");
        var itemE = new MenuItem("Show in Explorer");
        var itemC = new MenuItem("Copy path");

        var fileSelected = _selectedFile is not null;

        itemO.Sensitive = fileSelected;
        itemE.Sensitive = fileSelected;
        itemC.Sensitive = fileSelected;

        itemO.Activated += OpenFile;
        itemE.Activated += ShowFileInExplorer;
        itemC.Activated += CopyFilePath;

        menu.Add(itemO);
        menu.Add(itemE);
        menu.Add(itemC);

        menu.ShowAll();
        menu.Popup();
    }

    private void OpenFile(object? sender, EventArgs e)
    {
        if (_selectedFile is null) return;

        _fileOpener.OpenFileWithDefaultApp(_selectedFile.GetFullPath());
    }

    private void ShowFileInExplorer(object? sender, EventArgs e)
    {
        if (_selectedFile is null) return;

        _fileOpener.ShowFileInExplorer(_selectedFile.GetFullPath());
    }

    private async void CopyFilePath(object? sender, EventArgs args)
    {
        if (_selectedFile is null) return;

        await ClipboardService.SetTextAsync(_selectedFile.GetFullPath().Quote());
    }

    // FILES

    public async Task ShowFiles(List<MemeIndex_Core.Entities.File> files)
    {
        var store = CreateStore();
        await Task.Run(() => FillStore(store, files));
        Model = store;

        await Task.Run(UpdateFileIcons);
    }

    private static ListStore CreateStore()
    {
        var store = new ListStore(typeof(Pixbuf), typeof(string), typeof(string));

        return store;
    }

    private void FillStore(ListStore store, List<MemeIndex_Core.Entities.File> files)
    {
        store.Clear();

        _selectedFile = null;
        _files = files;

        foreach (var file in files)
        {
            store.AppendValues(null, file.Name, file.Directory.Path);
        }
    }

    private void UpdateFileIcons()
    {
        Model.Foreach((_, path, iter) =>
        {
            // (TRUE to stop iterating, FALSE to continue)

            if (_files is null || _files.Count == 0) return true;

            var index = path.Indices[0];
            if (index >= _files.Count) return false;

            var icon = GetImageIcon(_files[index].GetFullPath());
            if (icon is not null) Model.SetValue(iter, 0, icon);

            return false;
        });
    }

    private Pixbuf? GetImageIcon(string path)
    {
        try
        {
            var value = _iconCache.TryGetValue(path);
            if (value is not null) return value;

            using var stream = File.OpenRead(path);
            var icon = new Pixbuf(stream, 16, 16);

            _iconCache.Add(path, icon);
            return icon;
        }
        catch (Exception e)
        {
            Logger.LogError($"[{nameof(GetImageIcon)}][{path}]", e);
            return null;
        }
    }
}
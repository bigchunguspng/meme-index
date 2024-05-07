using Gdk;
using Gtk;
using Pango;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileTreeView : TreeView, IFileView
{
    private readonly FileViewUtils _utils = new();
    private readonly FileViewContextMenu _menu;

    private List<File>? _files;

    private File? SelectedFile
    {
        set => _menu.SelectedFile = value;
    }

    private App App { get; }

    public FileTreeView(App app)
    {
        App = app;

        _menu = new FileViewContextMenu(this);

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

        CursorChanged += (_, _) => SelectedFile = GetSelectedFile();
        RowActivated += (sender, _) => _menu.OpenFile(sender, EventArgs.Empty);
    }

    private File? GetSelectedFile()
    {
        if (_files is null) return null;

        var rows = Selection.GetSelectedRows();
        if (rows.Length == 0) return null;

        var index = rows[0].Indices[0];
        return _files.ElementAtOrDefault(index);
    }


    public async Task ShowFiles(List<File> files)
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

    private void FillStore(ListStore store, List<File> files)
    {
        store.Clear();

        SelectedFile = null;
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

            var icon = _utils.GetImageIcon(_files[index].GetFullPath(), 16).Result;
            if (icon is not null) Model.SetValue(iter, 0, icon);

            return false;
        });
    }

    public Widget AsWidget() => this;
}
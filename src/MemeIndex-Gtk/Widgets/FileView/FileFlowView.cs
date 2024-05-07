using Gtk;
using MemeIndex_Core.Utils;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileFlowView : FlowBox, IFileView
{
    private readonly FileViewUtils _utils = new();
    private readonly FileViewContextMenu _menu;

    private File? SelectedFile
    {
        get => _menu.SelectedFile;
        set => _menu.SelectedFile = value;
    }

    private readonly List<FileFlowBoxItem> _items = new();

    private App App { get; }

    public FileFlowView(App app)
    {
        App = app;

        _menu = new FileViewContextMenu(this);

        Orientation = Orientation.Horizontal;
        SelectionMode = SelectionMode.Browse;
        ColumnSpacing = 5;
        RowSpacing = 5;
        Homogeneous = true;
        ActivateOnSingleClick = false;

        SelectedChildrenChanged += (_, _) =>
        {
            if (SelectedChildren.Length > 0 && SelectedChildren[0].Children[0] is FileFlowBoxItem item)
            {
                SelectedFile = item.File;
                _menu.ShowFileDetailsInStatus();
            }
            else
            {
                SelectedFile = null;
            }
        };
        ChildActivated += (sender, _) => _menu.OpenFile(sender, EventArgs.Empty);
        ButtonPressEvent += (_, args) =>
        {
            if (args.Event.Button == 3)
            {
                var child = GetChildAtPos((int)args.Event.X, (int)args.Event.Y);
                if (child is not null) SelectChild(child);
            }
        };
    }

    public async Task ShowFiles(List<File> files)
    {
        SelectedFile = null;

        foreach (var item in _items) item.Parent.Destroy();

        _items.Clear();

        foreach (var file in files)
        {
            var item = new FileFlowBoxItem(file);
            Add(item);

            _items.Add(item);
        }

        ShowAll();

        await Task.Run(UpdateFileIcons);
    }
    
    private void UpdateFileIcons()
    {
        try
        {
            foreach (var item in _items)
            {
                var pixbuf = _utils.GetImageIcon(item.File.GetFullPath(), 96, 96);
                if (pixbuf is not null) item.SetIcon(pixbuf);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(UpdateFileIcons), e);
        }
    }

    public Widget AsWidget() => this;
}
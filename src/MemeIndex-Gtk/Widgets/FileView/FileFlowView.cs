using Gdk;
using Gtk;
using MemeIndex_Core.Utils;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileFlowView : FlowBox, IFileView
{
    private readonly FileViewUtils _utils = new();
    private readonly FileViewContextMenu _menu;
    private readonly ScrolledWindow _scroll;

    private bool _iconsLoaded;
    private Size AllocationSize;

    private File? SelectedFile
    {
        set => _menu.SelectedFile = value;
    }

    private readonly List<FileFlowBoxItem> _items = new();

    private App App { get; }

    public FileFlowView(App app, ScrolledWindow scroll)
    {
        App = app;

        _menu = new FileViewContextMenu(this);

        Orientation = Orientation.Horizontal;
        SelectionMode = SelectionMode.Single;
        ColumnSpacing = 5;
        RowSpacing = 5;
        Homogeneous = true;
        MaxChildrenPerLine = 100;
        Valign = Align.Start;
        ActivateOnSingleClick = false;

        _scroll = scroll;
        _scroll.Vadjustment.ValueChanged += (_, _) => UpdateFileIcons();

        SizeAllocated += UpdateIcons_OnSizeAllocated;
        SelectedChildrenChanged += OnSelectionChanged;
        ChildActivated += (sender, _) => _menu.OpenFile(sender, EventArgs.Empty);
        ButtonPressEvent += OnButtonPress;
    }

    private void UpdateIcons_OnSizeAllocated(object o, SizeAllocatedArgs sizeAllocatedArgs)
    {
        if (_iconsLoaded || Allocation.Size == AllocationSize) return;

        AllocationSize = Allocation.Size;
        UpdateFileIcons();
    }

    private void OnSelectionChanged(object? o, EventArgs eventArgs)
    {
        var selectedItem = SelectedChildren.Length > 0 ? SelectedChildren[0].Children[0] : null;
        SelectedFile = selectedItem is FileFlowBoxItem item ? item.File : null;
    }

    private void OnButtonPress(object o, ButtonPressEventArgs args)
    {
        var child = GetChildAtPos((int)args.Event.X, (int)args.Event.Y);
        if (child is null) UnselectAll();
        else if (args.Event.Button == 3) SelectChild(child);
    }


    private bool _updatingIcons, _iconsUpdateStopRequested;

    public async Task ShowFiles(List<File> files)
    {
        SelectedFile = null;
        AllocationSize = Size.Empty; // <-- to trigger icons update

        _iconsUpdateStopRequested = true;
        while (_updatingIcons) await Task.Delay(50);

        foreach (var item in _items) item.Parent.Destroy();

        _items.Clear();

        foreach (var file in files) _items.Add(new FileFlowBoxItem(file));
        foreach (var item in _items) Add(item);

        ShowAll();

        _iconsLoaded = false;
        _iconsUpdateStopRequested = false;
    }

    private async void UpdateFileIcons()
    {
        try
        {
            if (Parent is null) return; // widget not mounted

            _updatingIcons = true;

            var (skip, take) = GetRangeForIconsUpdate() ?? (0, 0);

            if (take == 0) return;

            foreach (var item in _items.Skip(skip).Take(take))
            {
                if (_iconsUpdateStopRequested) return;

                if (item.HasIcon) continue;

                var pixbuf = await _utils.GetImageIcon(item.File.GetFullPath(), 96);
                if (pixbuf is not null) item.SetIcon(pixbuf);
            }

            _iconsLoaded = _items.All(x => x.HasIcon);
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(UpdateFileIcons), e);
        }
        finally
        {
            _updatingIcons = false;
        }
    }

    private (int skip, int take)? GetRangeForIconsUpdate()
    {
        var lastWithIcon = _items.FindLastIndex(x => x.HasIcon);

        var skip = lastWithIcon + 1;
        if (skip == _items.Count) return null;

        var bottom = _scroll.Vadjustment.Value + _scroll.Vadjustment.PageSize;
        var last = _items.FindLastIndex(x => x.Allocation.Top <= bottom);
        if (last < skip) return null;

        var take = last - skip + 1;

#if DEBUG
        Logger.Log("[Large Icons / Update] skip: {0}", skip);
        Logger.Log("[Large Icons / Update] take: {0}", take);
#endif

        return (skip, take);
    }

    public Widget AsWidget() => this;
}
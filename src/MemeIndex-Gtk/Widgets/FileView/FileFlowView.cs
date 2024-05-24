using Gdk;
using Gtk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Gtk.Widgets.FileView;

public class FileFlowView : FlowBox, IFileView
{
    private readonly FileViewUtils _utils = new();
    private readonly FileViewContextMenu _menu;
    private readonly ScrolledWindow _scroll;

    private bool _iconsLoaded;
    private Size _allocationSize;

    private File? SelectedFile
    {
        set => _menu.SelectedFile = value;
    }

    private List<File> _files = [];
    private readonly List<FileFlowBoxItem> _items = [];

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
        Valign = Align.Fill;
        ActivateOnSingleClick = false;

        _scroll = scroll;
        _scroll.Vadjustment.ValueChanged += (_, _) => UpdateFileIcons();

        SizeAllocated += (_, _) => ShowHiddenFilesOnResize();
        SizeAllocated += UpdateIcons_OnSizeAllocated;
        SelectedChildrenChanged += OnSelectionChanged;
        ChildActivated += (sender, _) => _menu.OpenFile(sender, EventArgs.Empty);
        ButtonPressEvent += OnButtonPress_SelectItem;
    }

    private void UpdateIcons_OnSizeAllocated(object o, SizeAllocatedArgs sizeAllocatedArgs)
    {
        if (_iconsLoaded || Allocation.Size == _allocationSize) return;

        _allocationSize = Allocation.Size;
        UpdateFileIcons();
    }

    private void OnSelectionChanged(object? o, EventArgs eventArgs)
    {
        var selectedItem = SelectedChildren.Length > 0 ? SelectedChildren[0].Children[0] : null;
        SelectedFile = selectedItem is FileFlowBoxItem item ? item.File : null;
    }

    private void OnButtonPress_SelectItem(object o, ButtonPressEventArgs args)
    {
        var child = GetChildAtPos((int)args.Event.X, (int)args.Event.Y);
        if (child is null) UnselectAll();
        else if (args.Event.Button == 3) SelectChild(child);
    }


    private readonly StoppableProcessMonopoly _iconsUpdate = new();
    private long _requestId;
    private Dictionary<int, bool> _fileIsVisible = new();

    public async Task ShowFiles(List<File> files)
    {
        try
        {
            SelectedFile = null;
            _allocationSize = Size.Empty; // <-- to trigger icons update

            await _iconsUpdate.StopExternally();

            var requestId = UpdateData(files);

            ShowFileRange(requestId, take: GetMaxItemsOnScreen());

            ShowRestFilesAsync(requestId);
        }
        finally
        {
            _iconsLoaded = false;
            _iconsUpdate.AllowExternally();
        }
    }

    /// <returns>Id of the current files show request.</returns>
    private long UpdateData(List<File> files)
    {
        foreach (var item in _items)
        {
            item.Parent?.Destroy();
            item.Destroy();
        }

        _files = files;
        _fileIsVisible = files.ToDictionary(x => x.Id, _ => false);

        _items.Clear();

        return _requestId = DateTime.UtcNow.Ticks;
    }

    private async void ShowRestFilesAsync(long requestId)
    {
        await Task.Delay(2 * _files.Count); // 500 files -> 1 second

        if (_requestId != requestId) return;

        ShowFileRange(requestId, skip: GetMaxItemsOnScreen());
    }

    private void ShowHiddenFilesOnResize()
    {
        var firstHidden = _files.FindIndex(x => _fileIsVisible.TryGetValue(x.Id, out var visible) && !visible);
        if (firstHidden >= 0)
        {
            var difference = GetMaxItemsOnScreen() - firstHidden;
            if (difference > 0)
            {
                ShowFileRange(_requestId, skip: firstHidden, take: difference);
            }
        }

        UpdateFileIcons();
    }

    private void ShowFileRange(long requestId, int skip = 0, int take = 0)
    {
        foreach (var file in _files.Skip(skip).Take(take == 0 ? _files.Count : take))
        {
            if (_requestId != requestId) return;
            if (_fileIsVisible.TryGetValue(file.Id, out var visible) && visible) continue;

            var item = new FileFlowBoxItem(file);
            _items.Add(item);
            Add(item);
            if (item.Parent is not null) item.Parent.Valign = Align.Start;
            _fileIsVisible[file.Id] = true;
        }

        ShowAll();
    }

    private async void UpdateFileIcons()
    {
        try
        {
            if (Parent is null) return; // if widget is not mounted

            await _iconsUpdate.TakeRights();

            var (skip, take) = GetRangeForIconsUpdate() ?? (0, 0);

            if (take == 0) return;

            var tasks = _items.Skip(skip).Take(take).Select(async item =>
            {
                if (_iconsUpdate.ExecutionDisallowed || item.HasIcon) return;

                var pixbuf = await _utils.GetImageIcon(item.File.GetFullPath(), 96);
                if (pixbuf is not null) item.SetIcon(pixbuf);
            });
            await Task.WhenAll(tasks);

            _iconsLoaded = _items.All(x => x.HasIcon);
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(UpdateFileIcons), e);
        }
        finally
        {
            _iconsUpdate.ReleaseRights();
        }
    }

    private (int skip, int take)? GetRangeForIconsUpdate()
    {
        var lastWithIcon = _items.FindLastIndex(x => x.HasIcon);

        var skip = lastWithIcon + 1;
        if (skip == _items.Count) return null;

        var bottom = GetScrollBottom();
        var last = _items.FindLastIndex(x => x.Allocation.Top <= bottom);
        if (last < skip) return null;

        var take = last - skip + 1;

#if DEBUG
        Logger.Log("[Large Icons / Update] skip: {0}", skip);
        Logger.Log("[Large Icons / Update] take: {0}", take);
#endif

        return (skip, take);
    }

    private int GetMaxItemsOnScreen()
    {
        var w = AllocatedWidth;
        var h = _scroll.Vadjustment.PageSize.RoundToInt();
        var cols = (w + 5) / 117;
        var rows = (h + 5) / 137 + 1;
        return cols * rows;
    }

    private double GetScrollBottom() => _scroll.Vadjustment.Value + _scroll.Vadjustment.PageSize;

    public Widget AsWidget() => this;
}
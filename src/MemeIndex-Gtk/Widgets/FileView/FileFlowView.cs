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
    private Size AllocationSize;

    private File? SelectedFile
    {
        set => _menu.SelectedFile = value;
    }

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


    private readonly StoppableProcessMonopoly _iconsUpdate = new();

    private long _ticks;

    public async Task ShowFiles(List<File> files)
    {
        try
        {
            SelectedFile = null;
            AllocationSize = Size.Empty; // <-- to trigger icons update

            await _iconsUpdate.StopExternally();

            foreach (var item in _items) item.Parent?.Destroy();

            _items.Clear();

            var ticks  = DateTime.UtcNow.Ticks;
            _ticks = ticks;

            var max = GetMaxItemsOnScreen();
            Console.WriteLine(max.ToString());
            foreach (var file in files.Take(max))
            {
                var item = new FileFlowBoxItem(file);
                _items.Add(item);
                Add(item);
            }

            foreach (var child in Children) child.Valign = Align.Start;

            ShowAll();

            var last = Children.LastOrDefault();
            if (last is null) return;

            Children.Last().SizeAllocated += async (_, _) =>
            {
                await WaitForMouseToStop();

                if (_ticks != ticks) return;

                foreach (var file in files.Skip(max))
                {
                    if (_ticks != ticks) return;

                    var item = new FileFlowBoxItem(file);
                    _items.Add(item);
                    Add(item);
                }

                foreach (var child in Children) child.Valign = Align.Start;
                
                ShowAll();
            };
        }
        finally
        {
            _iconsLoaded = false;

            _iconsUpdate.AllowExternally();
        }
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

    private int _mouseX, _mouseY;
    private async Task WaitForMouseToStop()
    {
        while (true)
        {
            Window.GetDevicePosition(Display.DefaultSeat.Pointer, out var x, out var y, out _);
            if (_mouseX == x && _mouseY == y) return;

            await Task.Delay(750);
            _mouseX = x;
            _mouseY = y;
        }
    }

    public Widget AsWidget() => this;
}
using Gtk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Widgets.ColorSelection.Selectors;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk.Widgets.ColorSelection;

public class ColorSearchPanel : Frame
{
    [UI] private readonly Box _box = default!;

    private readonly     FunnyColorSelector _funnyColors;
    private readonly GrayscaleColorSelector  _grayColors;

    private readonly Button _buttonClearColorSelection;

    private bool _buttonsBeingManaged;

    private App App { get; }

    public HashSet<string> SelectedColors { get; } = [];

    public ColorSearchPanel(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        App = app;

        Realized += (_, _) => BuildPalette();

        _funnyColors = new FunnyColorSelector(App);
        _grayColors = new GrayscaleColorSelector(App);

        _buttonClearColorSelection = new Button { Label = "Clear selection", Expand = true };
        _buttonClearColorSelection.Clicked += DeactivateCheckboxes;
    }

    private void BuildPalette()
    {
        ConstructColorSelector(_funnyColors);
        ConstructColorSelector(_grayColors);

        var box = new Box(Orientation.Vertical, 0) { Expand = false };
        box.Add(_buttonClearColorSelection);
        _box.Add(box);
        
        ShowAll();
    }

    private void ConstructColorSelector(IColorSelector colorSelector)
    {
        colorSelector.Construct();
        _box.Add(colorSelector.AsWidget());
        colorSelector.AsWidget().ShowAll();
        foreach (var checkButton in colorSelector.CheckButtons)
        {
            checkButton.Toggled += CheckButtonOnToggled;
        }
    }

    private void CheckButtonOnToggled(object? sender, EventArgs e)
    {
        if (sender is not ColorCheckButton checkButton) return;

        checkButton.Active.Switch(SelectedColors.Add, SelectedColors.Remove)(checkButton.Key);

        if (_buttonsBeingManaged) return;

        SelectionChanged?.Invoke(this, e);
    }

    private void DeactivateCheckboxes(object? sender, EventArgs e)
    {
        _buttonsBeingManaged = true;

        var active = _grayColors.CheckButtons.GetActive()
            .Concat(_funnyColors.CheckButtons.GetActive());
        foreach (var checkButton in active) checkButton.Active = false;

        _buttonsBeingManaged = false;

        SelectionChanged?.Invoke(this, e);
    }

    public event EventHandler? SelectionChanged;
}
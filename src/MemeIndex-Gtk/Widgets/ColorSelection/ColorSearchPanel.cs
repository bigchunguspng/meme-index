using Gtk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using MemeIndex_Gtk.Widgets.ColorSelection.Selectors;

namespace MemeIndex_Gtk.Widgets.ColorSelection;

public class ColorSearchPanel : Frame
{
    private readonly Box _box;

    private readonly     FunnyColorSelector _funnyColors;
    private readonly GrayscaleColorSelector  _grayColors;

    private readonly Button _clearSelection;

    private bool _buttonsBeingManaged;

    private App App { get; }

    public HashSet<string> SelectedColors { get; } = [];

    public ColorSearchPanel(App app) : base("Color search")
    {
        App = app;

        Margin = 5;
        ShadowType = ShadowType.Out;

        LabelXalign = 0.5F;
        LabelWidget.MarginStart = 5;
        LabelWidget.MarginEnd = 5;

        _box = new Box(Orientation.Horizontal, 10) { Margin = 10 };
        Add(_box);

        _funnyColors = new FunnyColorSelector(App);
        _grayColors = new GrayscaleColorSelector(App);

        _clearSelection = new Button { Label = "Clear selection", Expand = true };
        _clearSelection.Clicked += DeactivateCheckboxes;

        Realized += (_, _) => BuildPalette();
    }

    private void BuildPalette()
    {
        ConstructColorSelector(_funnyColors);
        ConstructColorSelector(_grayColors);

        var box = new Box(Orientation.Vertical, 0) { Expand = false };
        box.Add(_clearSelection);
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
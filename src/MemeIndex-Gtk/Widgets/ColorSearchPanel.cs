using Gtk;
using MemeIndex_Core.Utils;
using MemeIndex_Gtk.Utils;
using UI = Gtk.Builder.ObjectAttribute;

namespace MemeIndex_Gtk.Widgets;

public class ColorSearchPanel : Frame
{
    [UI] private readonly Grid _gridColorsFunny = default!;
    [UI] private readonly Grid _gridColorsGray = default!;

    [UI] private readonly Button _buttonClearColorSelection = default!;

    private HashSet<string> SelectedColors = new();

    private App App { get; }

    public ColorSearchPanel(App app, WindowBuilder builder) : base(builder.Raw)
    {
        builder.Builder.Autoconnect(this);

        App = app;

        BuildPalette();

        _buttonClearColorSelection.Clicked += ClearColorSelectionOnClicked;
    }

    private void BuildPalette()
    {
        int top;
        var left = 0;
        foreach (var hue in App.ColorTagService.ColorsFunny)
        {
            top = 0;
            foreach (var color in hue.Value.Take(6))
            {
                AddColorCheckButton(top++, left, color.Key, _gridColorsFunny);
            }

            left++;
        }

        top = 0;
        foreach (var color in App.ColorTagService.ColorsGrayscale.Reverse())
        {
            AddColorCheckButton(top++, 0, color.Key, _gridColorsGray);
        }
    }

    private void AddColorCheckButton(int top, int left, string key, Grid grid)
    {
        var checkbutton = new ColorSearchCheckButton(key);
        checkbutton.Toggled += CheckbuttonOnToggled;
        grid.Attach(checkbutton, left, top, 1, 1);
    }

    private void CheckbuttonOnToggled(object? sender, EventArgs e)
    {
        if (sender is ColorSearchCheckButton checkButton)
        {
            checkButton.Active.Switch(SelectedColors.Add, SelectedColors.Remove)(checkButton.Key);

            App.SetStatus($"Selected colors: {string.Join(' ', SelectedColors)}");
        }
    }

    private void ClearColorSelectionOnClicked(object? sender, EventArgs e)
    {
        DeactivateCheckboxes(_gridColorsFunny);
        DeactivateCheckboxes(_gridColorsGray);
    }

    private void DeactivateCheckboxes(Container grid)
    {
        var active = grid.Children.OfType<ColorSearchCheckButton>().Where(x => x.Active);
        foreach (var checkButton in active) checkButton.Active = false;

        App.SetStatus($"Selected colors: {string.Join(' ', SelectedColors)}");
    }
}
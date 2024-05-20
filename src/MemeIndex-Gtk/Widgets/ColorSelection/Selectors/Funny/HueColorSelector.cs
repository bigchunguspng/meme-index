using Gtk;
using MemeIndex_Core.Services.ImageAnalysis.Color;

namespace MemeIndex_Gtk.Widgets.ColorSelection.Selectors.Funny;

public class HueColorSelector : ReverseBox
{
    public HueColorSelector
    (
        KeyValuePair<char, ColorPalette> hue,
        bool reverse = false
    )
        : base(Orientation.Horizontal, 2, reverse)
    {
        foreach (var color in reverse ? hue.Value.Reverse() : hue.Value)
        {
            AddColorCheckButton(color.Key);
        }
    }

    public List<ColorCheckButton> CheckButtons { get; } = new(6);

    private void AddColorCheckButton(string key)
    {
        var checkButton = new ColorCheckButton(key);
        Add(checkButton);
        CheckButtons.Add(checkButton);
    }
}
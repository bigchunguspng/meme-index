using Gtk;
using MemeIndex_Core.Services.ImageToText.ColorTag;

namespace MemeIndex_Gtk.Widgets.ColorSelection.Selectors.Funny;

public class HueGroupColorSelector : ReverseBox
{
    public HueGroupColorSelector
    (
        Dictionary<char, ColorPalette> hues,
        int size,
        bool reverseHues = false,
        bool reverseTones = false
    )
        : base(Orientation.Vertical, 2, reverseHues)
    {
        CheckButtons = new List<ColorCheckButton>(6 * size);

        foreach (var hue in reverseHues ? hues.Reverse() : hues)
        {
            AddHueColorSelector(hue, reverseTones);
        }
    }

    public List<ColorCheckButton> CheckButtons { get; }

    private void AddHueColorSelector(KeyValuePair<char, ColorPalette> hue, bool reverse = false)
    {
        var row = new HueColorSelector(hue, reverse);
        Add(row);
        CheckButtons.AddRange(row.CheckButtons);
    }
}
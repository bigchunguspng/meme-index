using Gtk;
using MemeIndex_Gtk.Widgets.ColorSelection.Selectors.Funny;

namespace MemeIndex_Gtk.Widgets.ColorSelection.Selectors;

public class FunnyColorSelector : Grid, IColorSelector
{
    private App App { get; }

    public FunnyColorSelector(App app)
    {
        App = app;

        RowSpacing = 2;
        ColumnSpacing = 8;

        Halign = Align.Start;
        Valign = Align.Start;

        RowHomogeneous = true;
        ColumnHomogeneous = true;
    }

    public List<ColorCheckButton> CheckButtons { get; } = new(6 * 3 * 4);

    public void Construct()
    {
        var funny = App.ColorSearchProfile.ColorsFunny;

        for (var i = 0; i < funny.Count / 3; i++)
        {
            var skip = (i + 2) % 4 * 3;
            var range = funny.Skip(skip).Take(3).ToDictionary();
            var reverseHues = i is 0 or 3;
            var reverseTones = i % 2 == 0;
            var selector = new HueGroupColorSelector(range, size: 3, reverseHues, reverseTones);
            Add(selector);
            CheckButtons.AddRange(selector.CheckButtons);
        }
    }

    public Widget AsWidget() => this;
}
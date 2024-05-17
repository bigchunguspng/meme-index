using Gtk;

namespace MemeIndex_Gtk.Widgets.ColorSelection.Selectors;

public class GrayscaleColorSelector : Grid, IColorSelector
{
    private App App { get; }

    public GrayscaleColorSelector(App app)
    {
        App = app;

        RowSpacing = 2;
        ColumnSpacing = 2;

        Halign = Align.Start;
        Valign = Align.Start;

        RowHomogeneous = true;
        ColumnHomogeneous = true;
    }

    public List<ColorCheckButton> CheckButtons { get; } = new(6);

    public void Construct()
    {
        var grayscale = App.ColorSearchProfile.ColorsGrayscale.ToList();

        for (var i = 0; i < grayscale.Count; i++)
        {
            var checkButton = new ColorCheckButton(grayscale[i].Key);
            Attach(checkButton, left: i / 3, top: i < 3 ? i : 2 - i % 3, 1, 1);
            CheckButtons.Add(checkButton);
        }
    }

    public Widget AsWidget() => this;
}
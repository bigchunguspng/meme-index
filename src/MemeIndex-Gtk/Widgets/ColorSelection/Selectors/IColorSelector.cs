using Gtk;

namespace MemeIndex_Gtk.Widgets.ColorSelection.Selectors;

public interface IColorSelector
{
    public void Construct();
    public List<ColorCheckButton> CheckButtons { get; }
    public Widget AsWidget();
}
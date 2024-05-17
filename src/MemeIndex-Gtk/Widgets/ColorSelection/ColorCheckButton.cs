using Gtk;

namespace MemeIndex_Gtk.Widgets.ColorSelection;

public class ColorCheckButton : CheckButton
{
    public string Key { get; }

    public ColorCheckButton(string key)
    {
        Key = key;

        Visible = true;
        FocusOnClick = false;

        StyleContext.AddClass("color");
        StyleContext.AddClass(key);
    }
}
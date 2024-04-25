using Gtk;

namespace MemeIndex_Gtk.Widgets;

public class ColorSearchCheckButton : CheckButton
{
    public string Key { get; }

    public ColorSearchCheckButton(string key)
    {
        Key = key;

        Visible = true;
        FocusOnClick = false;

        StyleContext.AddClass("color");
        StyleContext.AddClass(key);
    }
}
using Gtk;

namespace MemeIndex_Gtk.Utils;

/// <summary>
/// Use this to easily create <see cref="Window"/> from a <b>.glade file</b>.
/// </summary>
public class WindowBuilder
{
    public Builder Builder { get; }
    public IntPtr  Raw     { get; }

    /// <param name="key">Both name of the <b>.glade file</b> (without extension), and widget id.</param>
    public WindowBuilder(string key)
    {
        Builder = new($"{key}.glade");
        Raw = Builder.GetRawOwnedObject(key);
    }

    public void Autoconnect(object handler) => Builder.Autoconnect(handler);
}
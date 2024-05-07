using MemeIndex_Core;

namespace MemeIndex_Gtk;

public class ConfigGtk : Config
{
    /*
        color panel [remember / show/hide] : hide
        window size [remember / custom] : default
        window x y  [remember / custom] : center
     */

    public bool? ShowColorSearchPanel { get; set; }
    public bool? FileViewLargeIcons { get; set; }
    public Rectangle? WindowPosition { get; set; }
}

public record struct Rectangle(int X, int Y, int Width, int Height);
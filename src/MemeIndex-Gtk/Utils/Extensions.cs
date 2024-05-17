using Gtk;

namespace MemeIndex_Gtk.Utils;

public static class Extensions
{
    public static IEnumerable<T> GetActive<T>(this IEnumerable<T> checkButtons) where T : ToggleButton
    {
        return checkButtons.Where(x => x.Active);
    }
}
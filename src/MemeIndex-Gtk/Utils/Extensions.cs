using Gtk;

namespace MemeIndex_Gtk.Utils;

public static class Extensions
{
    public static IEnumerable<T> GetActive<T>(this IEnumerable<T> checkButtons) where T : ToggleButton
    {
        return checkButtons.Where(x => x.Active);
    }

    public static void PrintChildren(this Container container, string tabs = "")
    {
        var i = 0;
        foreach (var widget in container.Children)
        {
            Console.WriteLine($"{tabs}[{i++}] -> {widget.GetType().Name}");
            if (widget is Container con) con.PrintChildren(tabs+"\t");
        }
    }
}
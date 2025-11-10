namespace MemeIndex_Gtk.Utils;

public static class TextTrimmer
{
    public static string MakeTextFit(string text, int chars)
    {
        return text.Length < chars ? text : text[..(chars - 1)] + '…';
    }
}
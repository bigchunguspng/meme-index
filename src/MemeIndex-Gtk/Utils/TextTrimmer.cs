using System.Text;
using Gdk;
using Gtk;
using MemeIndex_Core.Utils;
using SixLabors.Fonts;

namespace MemeIndex_Gtk.Utils;

public static class TextTrimmer
{
    private static readonly TextOptions _options;

    // todo text this mf on linux

    static TextTrimmer()
    {
        var sw = Helpers.GetStartedStopwatch();
        var fontName = Settings.Default.FontName;
        var dpi = (float)Display.Default.DefaultScreen.Resolution;
        var separator = fontName.LastIndexOf(' ');
        var family = fontName.Remove(separator);
        var size = int.Parse(fontName[(separator + 1)..]);
        var font = new Font(SystemFonts.Get(family), size, FontStyle.Regular);
        _options = new TextOptions(font) { Dpi = dpi };
        sw.Log("Text trimmer");
    }

    public static string MakeTextFit(string text, float wantedWidth)
    {
        var span = (ReadOnlySpan<char>)text;

        var width = TextMeasurer.MeasureSize(span, _options).Width;

        if (width <= wantedWidth) return text;

        int min = 0, mid = 0, max = span.Length;
        while (min <= max)
        {
            mid = (min + max) >> 1;
            var slice = span[..mid];
            width = TextMeasurer.MeasureSize(slice, _options).Width;
            var space = wantedWidth - width;
            if /**/ (space < 12) max = mid - 1;
            else if (space > 36) min = mid + 1;
            else return new StringBuilder().Append(slice).Append('…').ToString();
        }

        return span[..mid].ToString();
    }
}
using System.Text;
using Gdk;
using Gtk;
using MemeIndex_Core.Services.ImageToText;
using MemeIndex_Core.Utils;

namespace MemeIndex_Gtk.Utils;

public class CustomCss
{
    private readonly ColorSearchProfile _colorSearchProfile;

    public CustomCss(ColorSearchProfile colorSearchProfile)
    {
        _colorSearchProfile = colorSearchProfile;
    }

    public void AddProviders()
    {
        var css1 = new CssProvider();
        var css2 = new CssProvider();
        css1.LoadFromResource("Style.css");
        css2.LoadFromData(GetColorSelectionCss());
        StyleContext.AddProviderForScreen(Screen.Default, css1, StyleProviderPriority.Application);
        StyleContext.AddProviderForScreen(Screen.Default, css2, StyleProviderPriority.Application);
    }

    private string GetColorSelectionCss()
    {
        var sb = new StringBuilder();
        foreach (var color in _colorSearchProfile.ColorsFunny.SelectMany(hue => hue.Value))
        {
            AppendStyle(sb, color.Key, color.Value);
        }

        foreach (var color in _colorSearchProfile.ColorsGrayscale.Where(x => x.Value.A > 0))
        {
            AppendStyle(sb, color.Key, color.Value);
        }

        return sb.ToString();
    }

    private static void AppendStyle(StringBuilder sb, string key, IronSoftware.Drawing.Color color)
    {
        var colorA = color                 .ToHtmlCssColorCode();
        var colorB = color.GetDarkerColor().ToHtmlCssColorCode();

        sb.Append("checkbutton.").Append(key).Append(" check ");
        sb.Append("{ ");
        sb.Append("background: "  ).Append(colorA).Append("; ");
        sb.Append("border-color: ").Append(colorB).Append("; ");
        sb.Append("} ");
    }
}
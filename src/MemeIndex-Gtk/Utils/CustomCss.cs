using System.Text;
using Gdk;
using Gtk;
using MemeIndex_Core.Services.ImageAnalysis.Color;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex_Gtk.Utils;

public class CustomCss(ColorSearchProfile colorSearchProfile)
{
    public void AddProviders()
    {
        var css1 = new CssProvider();
        var css2 = new CssProvider();
        css1.LoadFromResource("Style.css");
        css2.LoadFromData(GetColorSelectionCss());
        StyleContext.AddProviderForScreen(Screen.Default, css1, StyleProviderPriority.Application); // 0.11 sec
        StyleContext.AddProviderForScreen(Screen.Default, css2, StyleProviderPriority.Application);
    }

    private string GetColorSelectionCss()
    {
        var sb = new StringBuilder();
        foreach (var color in colorSearchProfile.ColorsFunny.SelectMany(hue => hue.Value))
        {
            AppendStyle(sb, color.Key, color.Value);
        }

        foreach (var color in colorSearchProfile.ColorsGrayscale)
        {
            AppendStyle(sb, color.Key, color.Value);
        }

        return sb.ToString();
    }

    private static void AppendStyle(StringBuilder sb, string key, Rgb24 color)
    {
        var colorA = color                 .ToCss();
        var colorB = color.GetDarkerColor().ToCss();

        sb.Append("checkbutton.").Append(key).Append(" check ");
        sb.Append("{ ");
        sb.Append("background: "  ).Append(colorA).Append("; ");
        sb.Append("border-color: ").Append(colorB).Append("; ");
        sb.Append("} ");
    }
}
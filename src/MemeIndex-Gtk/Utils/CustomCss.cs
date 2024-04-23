using System.Text;
using Gdk;
using Gtk;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Services.OCR;
using MemeIndex_Core.Utils;

namespace MemeIndex_Gtk.Utils;

public class CustomCss
{
    private readonly ColorTagService _colorTagService;

    public CustomCss(ColorTagService colorTagService)
    {
        _colorTagService = colorTagService;
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
        foreach (var color in _colorTagService.ColorsFunny.SelectMany(hue => hue.Value))
        {
            AppendStyle(sb, color.Key, color.Value);
        }

        foreach (var color in _colorTagService.ColorsGrayscale.Where(x => x.Value.A > 0))
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
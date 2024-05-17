using MemeIndex_Core.Services.ImageToText.ColorTag;
using MemeIndex_Core.Utils;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace MemeIndex_Console;

public static class Demo
{
    public static void ColorTagService_GetTextRepresentation(string path)
    {
        var sw = Helpers.GetStartedStopwatch();
        DebugTools.RenderHSL_Profile(path);
        sw.Log("DebugTools.RenderHSL_Profile(path);");

        var words = new ColorTagService(new ColorSearchProfile()).GetTextRepresentation(path).Result;
        sw.Log("GetTextRepresentation(path)");

        if (words is null) return;

        Console.WriteLine("COLORS FOUND: " + words.Count);
        foreach (var word in words)
        {
            Console.WriteLine(word.ToString());
        }
    }
}
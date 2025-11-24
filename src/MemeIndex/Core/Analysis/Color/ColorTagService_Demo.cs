using Spectre.Console;

namespace MemeIndex.Core.Analysis.Color;

public static class ColorTagService_Demo
{
    public static void Run(string path)
    {
        var sw = Stopwatch.StartNew();
        DebugTools.RenderProfile_HSL(path);
        sw.Log("\tDebugTools.RenderProfile_HSL(path);");

        DebugTools.RenderProfile_Oklch(path);
        sw.Log("\tDebugTools.RenderProfile_Oklch(path);");

        DebugTools.RenderProfile_Oklch_HxL(path);
        sw.Log("\tDebugTools.RenderProfile_Oklch_HxL(path);");

        return;
        var words = ColorTagService.GetTextRepresentation(path).Result;
        sw.Log("\tGetTextRepresentation(path)");

        if (words is null) return;

        Console.WriteLine("\nCOLORS FOUND: " + words.Count);
        var rows = Math.Ceiling(words.Count / 4.0);
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                var index = (int)(rows * col + row);
                if (index >= words.Count) break;

                var word = words[index];
                var rgb24 = word.Word[0] is >= 'A' and <= 'L'
                    ? ColorSearchProfile.ColorsFunny[word.Word[0]][word.Word]
                    : word.Word[0] is 'Y'
                        ? ColorSearchProfile.ColorsGrayscale[word.Word]
                        : 0.ToRgb24();
                var bg = ColorHelper.ColorConverter.RgbToHex(rgb24.ToRGB());
                var fg = (rgb24.R + rgb24.G + rgb24.B) / 3 > 127 ? "black" : "white";
                AnsiConsole.Markup($"[{fg} on #{bg}]\t#{word.Rank,7} - {word.Word,3}[/]");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
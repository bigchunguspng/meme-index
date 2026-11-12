namespace MemeIndex.Core.Analysis.Color.v1;

public static class ColorTagService_Demo
{
    public static void Run(string path)
    {
        var sw = Stopwatch.StartNew();
        DebugTools.RenderProfile_HSL(path);
        sw.LogCM(ConsoleColor.Yellow, "\tProfile - HSL");

        var words = ColorTagService.GetTextRepresentation(path).Result;
        sw.LogCM(ConsoleColor.Yellow, "\tImage Analysis");

        if (words is null) return;

        Console.WriteLine("\nCOLORS FOUND: " + words.Count);
        using var psb = new PooledStringBuilder();

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
                var cc_fg = (rgb24.R + rgb24.G + rgb24.B) / 3 > 127 ? CC_BLACK : CC_WHITE;
                psb.sb.BgRGB(rgb24).Color(cc_fg).Append($"\t#{word.Rank,7} - {word.Word,3}").Style();
            }

            psb.sb.Print();
            psb.sb.Clear();
        }

        Console.WriteLine();
    }
}
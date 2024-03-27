using System.Text.RegularExpressions;

namespace MemeIndex_Core.Utils;

public static partial class Helpers
{
    [GeneratedRegex("[\\s\\n\\r]+")]
    private static partial Regex LineBreaksRegex();

    public static string RemoveLineBreaks(this string text) => LineBreaksRegex().Replace(text, " ");
}
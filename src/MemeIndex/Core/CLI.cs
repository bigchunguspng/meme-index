using MemeIndex.Core.Analysis.Color;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.Utils;

namespace MemeIndex.Core;

public static class CLI
{
    public static readonly string
        NAME = Helpers.IsLinux
            ? "memeindex"
            : "MemeIndex",
        VERSION = $"{NAME} 0.3.3 ({Helpers.COMPILE_MODE})",
        HELP =
            $"""
             NAME:
                {NAME} - find memes on your machine

             SYNOPSIS:
                {NAME} [OPTIONS]...
                {NAME} [OPTIONS]... lab [OPTIONS-LAB]

             DESCRIPTION:
                This piece of software is in the development, please come back later.

             OPTIONS (common):
                    --dev                   Use DEVELOPMENT path scheme (everything next to binaries).
                    --ok                    Use regular     path scheme.

             OPTIONS (info):
                -!  --version               Show version info.
                -?  --help                  Show this screen.
                -/  --dirs                  Show app directories. 

             OPTIONS (web server):
                    --urls           URL;.. Listen to other URLs.
                -w  --web            PATH   Use other static web content directory.
                -l  --log                   Log all HTTP requests (might be slower).

             OPTIONS-LAB:
                -t  --test           INT    Execute method  from test list [1-9].
                -T  --test-list             List    methods from test list.
                -d  --demo    IMAGE-PATH... Analyze images, save report (image, tags, color profile).
                -D  --demo-list     FILE    Same as ^, take image paths from FILE.
                -p  --profile IMAGE-PATH... Save color profiles (all kinds).
                -P  --profile-list  FILE    Same as ^, take image paths from FILE.
                    --palette               Print color palette (JS syntax).
             """;

    private static void ShowHelpScreen()
    {
        using var reader = new StringReader(HELP);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith(' ')) Print(line);
            else Print(line, ConsoleColor.Yellow);
        }
    }

    private static Span<string> FilterArgs
        (string[] args_all, out bool lab)
    {
        var start = args_all.FindIndex(x => x is "lab") + 1;
        lab = start > 0;
        return args_all.AsSpan(start);
    }

    public static bool TryHandleArgs_Info
        (string[] args_all)
    {
        var args = FilterArgs(args_all, out var lab);
        if (lab)
            switch (args.Length)
            {
                case > 0 when args.ContainsAny("-T", "--list-tests"):
                    DebugTools.PrintTestOptions();
                    return true;
                default:
                    return false;
            }
        else
            switch (args.Length)
            {
                case > 0 when args.ContainsAny("-?", "--help"):
                    ShowHelpScreen();
                    return true;
                case > 0 when args.ContainsAny("-!", "--version"):
                    Print(VERSION);
                    return true;
                case > 0 when args.ContainsAny("-/", "--dirs"):
                    InspectDirectories();
                    return true;
                default:
                    return false;
            }
    }

    public static bool TryHandleArgs_Action
        (string[] args_all)
    {
        var args = FilterArgs(args_all, out var lab);
        if (lab)
            switch (args.Length)
            {
                case > 0 when args.Contains("--palette"):
                    DebugTools.PrintPalette();
                    return true;
                case > 1 when args.ContainsOption("-t", "--test", out var i):
                    var number = int.TryParse(args[i], out var n) ? n : 1;
                    DebugTools.Test(number);
                    return true;
                case > 1 when args.ContainsOption("-p", "--profile", out var i):
                    args.Slice(start: i)
                        .ForEachTry(DebugTools.RenderAllProfiles);
                    return true;
                case > 1 when args.ContainsOption("-P", "--profile-list", out var i):
                    GetArgsFromFile(args[i])
                        .ForEachTry(DebugTools.RenderAllProfiles);
                    return true;
                case > 1 when args.ContainsOption("-d", "--demo", out var i):
                    args.Slice(start: i)
                        .ForEachTry(ColorTagger_v2_Demo.Run);
                    return true;
                case > 1 when args.ContainsOption("-D", "--demo-list", out var i):
                    GetArgsFromFile(args[i])
                        .ForEachTry(ColorTagger_v2_Demo.Run);
                    return true;
                default:
                    return false;
            }
        else
            return false;
    }

    public static bool ContainsOption
        (this Span<string> args, string alias, string option, out int value_index)
    {
        value_index = args.IndexOfAny(alias, option) + 1;
        return value_index > 0 && value_index < args.Length;
    }

    /// Write a list of args in a file.
    /// One line - one argument.
    /// Start line with "#" to skip it or "==" to stop parsing altogether.
    public static IEnumerable<string> GetArgsFromFile
        (string path) =>
        File.ReadAllLines(path)
            .TakeWhile(s => s.StartsWith("==").Janai())
            .Where    (s => s.IsNotNull_NorWhiteSpace()
                         && s.StartsWith('#') .Janai())
            .Select   (s => s.Trim('"'));
}
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
                {NAME}     [OPTIONS]...
                   Run in normal mode (web server).
                {NAME} lab [OPTIONS]...
                   Run in    lab mode (experiments / debug).

             DESCRIPTION:
                This piece of software is in the development, please come back later.

             OPTIONS (normal mode):
                    --urls           URL;.. Listen to other URLs.
                -l  --log                   Log all HTTP requests (might be slower).
                -!  --version               Show version info.
                -?  --help                  Show this screen.

             OPTIONS (lab):
                -t  --test           INT    Execute method  from test list [1-9].
                -T  --test-list             List    methods from test list.
                -d  --demo    IMAGE-PATH... Analyze images, save report (image, tags, color profile).
                -D  --demo-list     FILE    Same as ^, take image paths from FILE.
                -p  --profile IMAGE-PATH... Save color profiles (all kinds).
                -P  --profile-list  FILE    Same as ^, take image paths from FILE.
             """;

    private static Span<string> FilterArgs
        (string[] args_all, out bool lab)
    {
        lab  = args_all.Length > 1 && args_all[0] == "lab";
        return args_all.AsSpan(start: lab ? 1 : 0);
    }

    public static bool TryHandleArgs_Info
        (string[] args_all)
    {
        var args = FilterArgs(args_all, out var lab);
        if (lab)
            switch (args.Length)
            {
                case > 0 when args[0] is "-T" or "--list-tests":
                    DebugTools.PrintTestOptions();
                    return true;
                default:
                    return false;
            }
        else
            switch (args.Length)
            {
                case > 0 when args[0] is "-?" or "--help":
                    Print(HELP);
                    return true;
                case > 0 when args[0] is "-!" or "--version":
                    Print(VERSION);
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
                case > 0 when args[0] is "-t" or "--test":
                    var number = args.Length > 1 && int.TryParse(args[1], out var n) ? n : 1;
                    DebugTools.Test(number);
                    return true;
                case > 1 when args[0] is "-p" or "--profile":
                    args.Slice(start: 1)
                        .ForEachTry(DebugTools.RenderAllProfiles);
                    return true;
                case > 1 when args[0] is "-P" or "--profile-list":
                    GetArgsFromFile(args[1])
                        .ForEachTry(DebugTools.RenderAllProfiles);
                    return true;
                case > 1 when args[0] is "-d" or "--demo":
                    args.Slice(start: 1)
                        .ForEachTry(ColorTagger_v2_Demo.Run);
                    return true;
                case > 1 when args[0] is "-D" or "--demo-list":
                    GetArgsFromFile(args[1])
                        .ForEachTry(ColorTagger_v2_Demo.Run);
                    return true;
                default:
                    return false;
            }
        else
            return false;
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
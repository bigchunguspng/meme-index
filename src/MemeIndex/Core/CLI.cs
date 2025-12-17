using MemeIndex.Core.Analysis.Color;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.Utils;

namespace MemeIndex.Core;

public static class CLI
{
    private static readonly string
        NAME = Helpers.IsWindows
            ? "MemeIndex"
            : "memeindex",
        VERSION = $"{NAME} 0.3.3 ({Helpers.COMPILE_MODE})",
        HELP =
            $"""
             NAME:
                {NAME} - find memes on your machine

             SYNOPSIS:
                {NAME}
                   Run in normal mode (web server).
                {NAME} [OPTIONS]...
                   Run in other modes.

             DESCRIPTION:
                This piece of software is in the development, please come back later.

             OPTIONS:
                -t  --test                  Execute whatever is in the test method.
                -d  --demo    IMAGE-PATH... Analyze images, print result to console, save HSL profile.
                -D  --demo-list     FILE    Same as above, image paths are taken from FILE.
                -p  --profile IMAGE-PATH... Save image profiles (all kinds).
                -P  --profile-list  FILE    Same as above, image paths are taken from FILE.
                -!  --version               Show version info.
                -?  --help                  Show this screen.
             """;

    public static bool TryHandleArgs_HelpOrVersion
        (string[] args)
    {
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

    public static bool TryHandleArgs_Other
        (string[] args)
    {
        //Log("ARGS: " + string.Join(", ", args), color: ConsoleColor.Yellow);
        switch (args.Length)
        {
            case > 0 when args[0] is "-t" or "--test":
                var number = args.Length > 1 && int.TryParse(args[1], out var n) ? n : 1;
                DebugTools.Test(number);
                return true;
            case > 1 when args[0] is "-p" or "--profile":
                args.Skip(1)
                    .ForEachTry(DebugTools.RenderAllProfiles);
                return true;
            case > 1 when args[0] is "-P" or "--profile-list":
                GetArgsFromFile(args[1])
                    .ForEachTry(DebugTools.RenderAllProfiles);
                return true;
            case > 1 when args[0] is "-d" or "--demo":
                args.Skip(1)
                    .ForEachTry(ColorTagger_v2_Demo.Run);
                return true;
            case > 1 when args[0] is "-D" or "--demo-list":
                GetArgsFromFile(args[1])
                    .ForEachTry(ColorTagger_v2_Demo.Run);
                return true;
            default:
                return false;
        }
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
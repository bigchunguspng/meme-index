namespace MemeIndex.Core;

public static class Config
{
    /// Store all data next to app executable.
    public static bool DEVELOPMENT;

    /// Alternative web root.
    public static FilePath? WEB_ROOT;

    public static void ConfigureDirectories(Span<string> args)
    {
        DEVELOPMENT = args.Contains("--dev");

        if (args.ContainsOption("-w", "--web", out var i))
            WEB_ROOT = args[i];
    }
}
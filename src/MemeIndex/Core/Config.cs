namespace MemeIndex.Core;

public static class Config
{
    /// Store all data next to app executable.
    public static bool DEVELOPMENT;

    /// Alternative web root.
    public static FilePath? WEB_ROOT;

    public static void ConfigureDirectories(Span<string> args)
    {
        if      (args.Contains("--ok"))
            DEVELOPMENT = false;
        else if (args.Contains("--dev") || Environment.GetEnvironmentVariable("MEMEINDEX_DEVELOPMENT") != null)
            DEVELOPMENT = true;

        if (args.ContainsOption("-w", "--web", out var i))
            WEB_ROOT = args[i];
    }
}
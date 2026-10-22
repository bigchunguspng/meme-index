using static System.StringSplitOptions;

namespace MemeIndex.Core;

public static class Config
{
    public static void ConfigureDirectories(Span<string> args)
    {
        DEVELOPMENT
            = args.ContainsOption("-d", "--dev")
           || Environment.GetEnvironmentVariable("MEME_INDEX_DEV") != null;
        WEB_ROOT
            = args.ContainsOption("-w", "--web", out var i)
            ? args[i]
            : Environment.GetEnvironmentVariable("MEME_INDEX_WEB").MakeNull_IfWhiteSpace();
    }

    // CLI ARGS | ENV VARS
    public static bool         DEVELOPMENT        { get; private set; } // Store all data next to app executable.
    public static string?      WEB_ROOT           { get; private set; } // Alternative web root.

    // == MONITORING
    public static bool         Monitoring_Enabled { get; private set; } = true;
    public static TimeSpan     Monitoring_Delay   { get; private set; } = TimeSpan.FromSeconds(5);
    // == SYNC_START
    public static bool         StartupSync_Enabled{ get; private set; } = true;
    public static TimeSpan     StartupSync_Delay  { get; private set; } = TimeSpan.FromSeconds(0);

    // READ / WRITE

    private const string
        SECTION_MONITORING = "MONITORING",
        SECTION_SYNC_START = "SYNC_START",
        PROP_ENABLED = "on",
        PROP_DELAY   = "delay";

    private enum Section { None, Miku, Sync, }

    private static readonly SemaphoreSlim _s = new(1, 1);

    public static async Task Read() => await _s.Lock(async () =>
    {
        if (File_Config.FileExists.Janai())
        {
            await Write_Internal();
            return;
        }

        var lines = await File.ReadAllLinesAsync(File_Config);
        var section = Section.None;
        Span<Range> bits = stackalloc Range[2];
        for (var i = 0; i < lines.Length; i++)
        {
            var line_raw = lines[i];
            var line = line_raw.Trim();
            if (line.IsNull_OrWhiteSpace()) continue;
            if (line.StartsWith('#'  ))     continue;
            if (line.StartsWith("== "))
            {
                var section_name = line[3..];
                section
                    = section_name == SECTION_SYNC_START ? Section.Sync
                    : section_name == SECTION_MONITORING ? Section.Miku : Section.None;
            }
            else if (section != Section.None)
            {
                var count = line.Split(bits, '=', TrimEntries | RemoveEmptyEntries);
                if (count < 2)
                {
                    LogWarn(NAME_R, WARN_LINE_INVALID.Format(i, line_raw));
                    continue;
                }

                var (name, value) = (line[bits[0]], line[bits[1]]);
                if      (section is Section.Miku)
                {
                    if      (name == PROP_ENABLED) Monitoring_Enabled  = value.Parse_Bool();
                    else if (name == PROP_DELAY)   Monitoring_Delay    = value.Parse_Time(i, line_raw, Monitoring_Delay);
                }
                else if (section is Section.Sync)
                {
                    if      (name == PROP_ENABLED) StartupSync_Enabled = value.Parse_Bool();
                    else if (name == PROP_DELAY)   StartupSync_Delay   = value.Parse_Time(i, line_raw, StartupSync_Delay);
                }
            }
            else
                LogWarn(NAME_R, WARN_LINE_IGNORED.Format(i, line_raw));
        }
    });

    public static async Task Write() => await _s.Lock(async () =>
    {
        await Write_Internal();
    });

    private static async Task Write_Internal()
    {
        string[] lines =
        [
            $"# MemeIndex {CLI.VERSION} User Config File",
            // "# Edit and save this file to reload the config (may need restart).",
            "",
            $"== {SECTION_MONITORING}",
            $"{PROP_ENABLED} = {Monitoring_Enabled .Encode()}",
            $"{PROP_DELAY  } = {Monitoring_Delay   .Encode()}",
            "",
            $"== {SECTION_SYNC_START}",
            $"{PROP_ENABLED} = {StartupSync_Enabled.Encode()}",
            $"{PROP_DELAY  } = {StartupSync_Delay  .Encode()}",
        ];
        await File.WriteAllLinesAsync(File_Config, lines);
        Log(NAME_W, "CONFIG FILE UPDATED!");
    }

    // UPDATE

    public static bool _restart_needed;

    public static async Task<bool> Update( /* data */ ) => await _s.Lock(async () =>
    {
        // todo update values and trigger changes
        _restart_needed = false;

        await Write_Internal();
    }).ContinueWith(_ => _restart_needed);

    // ENCODE

    private static string Encode
        (this bool b) => b ? "Y" : "N";

    private static bool Parse_Bool
        (this string s) => s[0] is 'Y' or 'y';

    private static string Encode(this TimeSpan t)
    {
        var u
            = t.Milliseconds > 0 ? "ms"
            : t.Seconds      > 0 ? "s"
            : t.Minutes      > 0 ? "m"
            : t.Hours        > 0 ? "h"
            : t.Days         > 0 ? "d" : "ms";
        return u switch
        {
            "s"  => $"{t.TotalSeconds}s",
            "m"  => $"{t.TotalMinutes}m",
            "h"  => $"{t.TotalHours}h",
            "d"  => $"{t.TotalDays}d",
            _    => $"{t.TotalMilliseconds}ms",
        };
    }

    private static TimeSpan Parse_Time(this string s, int i, string line, TimeSpan fallback)
    {
        var i_unit = s.IndexOfAny("smhd".AsSpan());
        if (i_unit < 0) goto INVALID;

        var u = s.AsSpan(i_unit);
        if (double.TryParse(s.AsSpan(0, i_unit), out var value)) return u switch
        {
            "s"  => TimeSpan.FromSeconds     (value),
            "m"  => TimeSpan.FromMinutes     (value),
            "h"  => TimeSpan.FromHours       (value),
            "d"  => TimeSpan.FromDays        (value),
            _    => TimeSpan.FromMilliseconds(value),
        };

        INVALID:
        LogWarn(NAME_R, WARN_TIME_INVALID.Format(i, line, fallback.Encode()));
        return fallback;
    }

    private const string
        NAME_R = "[Config|R]",
        NAME_W = "[Config|W]",
        WARN_LINE_IGNORED = "Line {0,2} wasn't parsed beacuse no section header was found before it:\n{1}",
        WARN_LINE_INVALID = "Line {0,2} wasn't parsed beacuse no name or/and value was found:\n{1}",
        WARN_TIME_INVALID = "Line {0,2} doesn't contain a valid duration value, using default one ({2}):\n{1}";
}
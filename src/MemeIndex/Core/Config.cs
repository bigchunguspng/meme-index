using MemeIndex.Core.Indexing;
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
    public static bool         DEVELOPMENT         { get; private set; } // Store all data next to app executable.
    public static string?      WEB_ROOT            { get; private set; } // Alternative web root.

    // == MONITORING
    public static bool         Monitoring_Enabled  { get; private set; } = true;
    public static TimeSpan     Monitoring_Delay    { get; private set; } = TimeSpan.FromSeconds(5);
    // == SYNC_START
    public static bool         StartupSync_Enabled { get; private set; } = true;
    public static TimeSpan     StartupSync_Delay   { get; private set; } = TimeSpan.FromSeconds(0);

    private static API_Config?    _tree;
    private static API_Config      Tree => _tree ??= BuildTree();
    private static API_Config BuildTree() => new()
    {
        [SECTION_MONITORING] =
        {
            [PROP_ENABLED] = Monitoring_Enabled.Encode(),
            [PROP_DELAY  ] = Monitoring_Delay.Encode(),
        },
        [SECTION_SYNC_START] =
        {
            [PROP_ENABLED] = StartupSync_Enabled.Encode(),
            [PROP_DELAY  ] = StartupSync_Delay.Encode(),
        }
    };

    private const string
        SECTION_MONITORING = "MONITORING",
        SECTION_SYNC_START = "SYNC_START",
        PROP_ENABLED = "on",
        PROP_DELAY   = "delay";

    private static readonly SemaphoreSlim _s = new(1, 1);

    // FILE - READ / WRITE

    public static async Task File_Read() => await _s.Lock(async () =>
    {
        if (File_Config.FileExists.Janai())
        {
            await File_Write();
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
                section = GetSection(line[3..]);
            }
            else if (section != Section.None)
            {
                var count = line.Split(bits, '=', TrimEntries | RemoveEmptyEntries);
                if (count == 2)
                {
                    var name  = line[bits[0]];
                    var value = line[bits[1]];
                    SetProperty(section, name, value, i);
                }
                else
                    LogWarn(NAME_R, WARN_LINE_INVALID.Format(i, line_raw));
            }
            else
                LogWarn(NAME_R, WARN_LINE_IGNORED.Format(i, line_raw));
        }
    });

    private static async Task File_Write()
    {
        await File.WriteAllLinesAsync(File_Config, GetConfigLines());
        Log(NAME_W, "CONFIG FILE UPDATED!");
    }

    private static IEnumerable<string> GetConfigLines()
    {
        yield return $"# MemeIndex {CLI.VERSION} User Config File";
        // yield return  "# Edit and save this file to reload the config (may need restart).";
        
        foreach (var (section, props) in Tree)
        {
            yield return "";
            yield return $"== {section}";
            foreach (var (name, value) in props)
            {
                yield return $"{name} = {value}";
            }
        }
    }

    // API - GET / UPDATE

    public static async Task<API_Config>
        API_Get
        () => await _s.Lock(() => Tree);

    public static async Task<bool>
        API_Update
        (API_Config data) => await _s.Lock(async () =>
    {
        _tree = null; // invalidate cache

        var i = 0;
        foreach (var (section_name, props) in data)
        {
            var section = GetSection(section_name);
            foreach (var (name, value) in props)
            {
                SetProperty(section, name, value, i++);
            }
        }

        await Task.WhenAll(File_Write(), Miku.FollowConfig());

        var restart_needed = false;

        return restart_needed;
    });

    // ROUTE

    private enum Section { None, Miku, Sync, }

    private static Section GetSection(string s) => s switch
    {
        SECTION_SYNC_START => Section.Sync,
        SECTION_MONITORING => Section.Miku,
        _                  => Section.None
    };

    private static void SetProperty
        (Section section, string name, string value, int i)
    {
        if      (section is Section.Miku)
        {
            if      (name == PROP_ENABLED) Monitoring_Enabled  = value.Parse_Bool();
            else if (name == PROP_DELAY)   Monitoring_Delay    = value.Parse_Time(i, name, Monitoring_Delay);
        }
        else if (section is Section.Sync)
        {
            if      (name == PROP_ENABLED) StartupSync_Enabled = value.Parse_Bool();
            else if (name == PROP_DELAY)   StartupSync_Delay   = value.Parse_Time(i, name, StartupSync_Delay);
        }
    }

    // ENCODE / PARSE

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

    private static TimeSpan Parse_Time
        (this string s, int i, string name, TimeSpan fallback)
    {
        var match = _r_time.Match(s);
        if (match.Failed())
        {
            LogWarn(NAME_R, WARN_TIME_INVALID.Format(i, name, s, fallback.Encode()));
            return fallback;
        }
        var value = match.ExtractGroup(1, double.Parse);
        var units = match.ExtractGroup(2, x => x, "ms");
        return units switch
        {
            "s" => TimeSpan.FromSeconds     (value),
            "m" => TimeSpan.FromMinutes     (value),
            "h" => TimeSpan.FromHours       (value),
            "d" => TimeSpan.FromDays        (value),
            _   => TimeSpan.FromMilliseconds(value),
        };
    }

    private static readonly Regex
        _r_time = new(@"^(\d+)(ms|s|m|h|d)?$", RegexOptions.Compiled);

    private const string
        NAME_R = "[Config|R]",
        NAME_W = "[Config|W]",
        WARN_LINE_IGNORED = "Line {0,2} wasn't parsed beacuse no section header was found before it:\n{1}",
        WARN_LINE_INVALID = "Line {0,2} wasn't parsed beacuse no name or/and value was found:\n{1}",
        WARN_TIME_INVALID = "Line {0,2} doesn't contain a valid duration value, using default one ({3}):\nNAME: {1}\nVALUE: {2}";
}

public class API_Config : Dictionary<string, Dictionary<string, string>>;
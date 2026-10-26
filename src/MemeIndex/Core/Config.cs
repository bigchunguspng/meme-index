using MemeIndex.Core.Indexing;
using static System.StringSplitOptions;
using LogLevel = MemeIndex.Tools.Logging.LogLevel;

namespace MemeIndex.Core;

// ReSharper disable AssignmentInConditionalExpression

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
    public  static bool        DEVELOPMENT         { get; private set; } // Store all data next to app executable.
    public  static string?     WEB_ROOT            { get; private set; } // Alternative web root.

    // == SERVER
    private static int         Server_MinLogLevel  { get;         set; }
    public  static string[]?   Server_URLs         { get; private set; }
    // == SYNC
    public  static bool        Sync_Enabled  { get; private set; } = true;
    // == MONITORING
    public  static bool        Monitoring_Enabled  { get; private set; } = true;
    public  static TimeSpan    Monitoring_Delay    { get; private set; } = TimeSpan.FromSeconds(5);
    // == SYNC_START
    public  static bool        StartupSync_Enabled { get; private set; } = true;
    public  static TimeSpan    StartupSync_Delay   { get; private set; } = TimeSpan.FromSeconds(0);

    private static API_Config?    _tree;
    private static API_Config      Tree => _tree ??= BuildTree();
    private static API_Config BuildTree()
    {
        var result = new API_Config();
        var s_serv = result[SECTION_SERVER    ] = new Dictionary<string, string>();
        var s_sync = result[SECTION_SYNC      ] = new Dictionary<string, string>();
        var s_ss   = result[SECTION_SYNC_START] = new Dictionary<string, string>();
        var s_miku = result[SECTION_MONITORING] = new Dictionary<string, string>();
        s_serv[PROP_LOG_LVL] =      Server_MinLogLevel .Encode();
        s_serv[PROP_URLS   ] =      Server_URLs        .Encode();
        s_sync[PROP_ENABLED] =        Sync_Enabled     .Encode();
        s_ss  [PROP_ENABLED] = StartupSync_Enabled     .Encode();
        s_ss  [PROP_DELAY  ] = StartupSync_Delay       .Encode();
        s_miku[PROP_ENABLED] =  Monitoring_Enabled     .Encode();
        s_miku[PROP_DELAY  ] =  Monitoring_Delay       .Encode();
        return result;
    }

    private static bool SetProperty
        (string section, string name, string value, int ln)
    {
        var b = false;
        if      (section is SECTION_SERVER)
        {
            if      (b = name == PROP_LOG_LVL) Server_MinLogLevel  = value.Parse_Int(ln, name, Server_MinLogLevel);
            else if (b = name == PROP_URLS)    Server_URLs         = value.Parse_StringArray();
        }
        else if (section is SECTION_SYNC)
        {
            if      (b = name == PROP_ENABLED) Sync_Enabled        = value.Parse_Bool();
        }
        else if (section is SECTION_SYNC_START)
        {
            if      (b = name == PROP_ENABLED) StartupSync_Enabled = value.Parse_Bool();
            else if (b = name == PROP_DELAY)   StartupSync_Delay   = value.Parse_Time(ln, name, StartupSync_Delay);
        }
        else if (section is SECTION_MONITORING)
        {
            if      (b = name == PROP_ENABLED) Monitoring_Enabled  = value.Parse_Bool();
            else if (b = name == PROP_DELAY)   Monitoring_Delay    = value.Parse_Time(ln, name, Monitoring_Delay);
        }

        return b;
    }

    private const string
        SECTION_SERVER     = "SERVER",
        SECTION_SYNC       = "SYNC",
        SECTION_SYNC_START = "STARTUP SYNC",
        SECTION_MONITORING = "MONITORING",
        PROP_LOG_LVL = "log level",
        PROP_URLS    = "URLs",
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
        var section = "";
        Span<Range> bits = stackalloc Range[2];
        for (var i = 0; i < lines.Length; i++)
        {
            var ln = i + 1;
            var line_raw = lines[i];
            var line = line_raw.Trim();
            if (line.IsNull_OrWhiteSpace()) continue;
            if (line.StartsWith('#'  ))     continue;
            if (line.StartsWith("== "))
            {
                section = line[3..];
            }
            else if (section != "")
            {
                var count = line.Split(bits, '=', TrimEntries | RemoveEmptyEntries);
                if (count == 2)
                {
                    var name  = line[bits[0]];
                    var value = line[bits[1]];
                    if (SetProperty(section, name, value, ln).Failed())
                        LogWarn(NAME_R, WARN_LINE_UNKNOWN.Format(ln, line_raw));
                }
                else
                    LogWarn(NAME_R, WARN_LINE_INVALID.Format(ln, line_raw));
            }
            else
                LogWarn(NAME_R, WARN_LINE_ORPHAN.Format(ln, line_raw));
        }
        ApplyConfig();
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

    private static bool _restart_needed;

    public static async Task<API_Config>
        API_Get
        () => await _s.Lock(() => Tree);

    public static async Task<bool>
        API_Update
        (API_Config data) => await _s.Lock(async () =>
    {
        var prev_urls = Server_URLs;

        _tree = null; // invalidate cache

        var ln = 1;
        foreach (var (section, props) in data)
        {
            foreach (var (name, value) in props)
            {
                SetProperty(section, name, value, ln++);
            }
        }
        ApplyConfig();

        await Task.WhenAll(File_Write(), Miku.FollowConfig());

        if (_restart_needed
         || prev_urls is null != Server_URLs is null
         || prev_urls != null && Server_URLs != null
         && prev_urls.Order().SequenceEqual(Server_URLs.Order()))
            _restart_needed = true;

        return _restart_needed;
    });

    private static void ApplyConfig()
    {
        MinLogLevel = (LogLevel)Server_MinLogLevel;
    }

    // ENCODE / PARSE

    private static string Encode
        (this bool b) => b ? "Y" : "N";

    private static bool Parse_Bool
        (this string s) => s[0] is 'Y' or 'y';

    private static string Encode
        (this int n) => n.ToString();

    private static int Parse_Int
        (this string s, int ln, string name, int fallback)
    {
        if (int.TryParse(s, out var n)) return n;

        LogWarn(NAME_R, WARN_INT_INVALID.Format(ln, name, s, fallback.Encode()));
        return fallback;
    }

    private static string Encode
        (this string[]? a) => a != null ? string.Join(';', a) : "-";

    private static string[]? Parse_StringArray
        (this string s) => s == "-" ? null : s.Split(';');

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
        (this string s, int ln, string name, TimeSpan fallback)
    {
        var match = _r_time.Match(s);
        if (match.Failed())
        {
            LogWarn(NAME_R, WARN_TIME_INVALID.Format(ln, name, s, fallback.Encode()));
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
        WARN_LINE_ORPHAN  = "Line {0,2} - ignored - no section name found before it:\n{1}",
        WARN_LINE_INVALID = "Line {0,2} - ignored - no name or/and value found:\n{1}",
        WARN_LINE_UNKNOWN = "Line {0,2} - ignored - unknown property:\n{1}",
        WARN_INT_INVALID  = "Line {0,2} - not a valid integer"  + ":\nNAME: {1}\nVALUE: {2}\nVALUE USED: {3}",
        WARN_TIME_INVALID = "Line {0,2} - not a valid duration" + ":\nNAME: {1}\nVALUE: {2}\nVALUE USED: {3}";
}

public class API_Config : Dictionary<string, Dictionary<string, string>>;
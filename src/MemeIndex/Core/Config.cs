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

    // CONFIG FILE
    public static int         Server_MinLogLevel  { get; private set; }
    public static string[]?   Server_URLs         { get; private set; }
    public static bool        Sync_Enabled        { get; private set; } = true;
    public static bool        Monitoring_Enabled  { get; private set; } = true;
    public static TimeSpan    Monitoring_Delay    { get; private set; } = TimeSpan.FromSeconds(5);
    public static bool        StartupSync_Enabled { get; private set; } = true;
    public static TimeSpan    StartupSync_Delay   { get; private set; } = TimeSpan.FromSeconds(0);

    private const string
        s_SERV = "SERVER",
        s_SYNC = "SYNC",
        s_SSUP = "STARTUP SYNC",
        s_MIKU = "MONITORING",
        p_LOG_L = "log level",
        p_URLS  = "URLs",
        p_ON    = "on",
        p_DELAY = "delay";

    private static API_Config?    _tree;
    private static API_Config      Tree => _tree ??= BuildTree();
    private static API_Config BuildTree()
    {
        var result = new API_Config();
        var s_serv = result[s_SERV] = new Dictionary<string, string>();
        var s_sync = result[s_SYNC] = new Dictionary<string, string>();
        var s_ssup = result[s_SSUP] = new Dictionary<string, string>();
        var s_miku = result[s_MIKU] = new Dictionary<string, string>();
        s_serv[p_LOG_L] =      Server_MinLogLevel .Encode();
        s_serv[p_URLS ] =      Server_URLs        .Encode();
        s_sync[p_ON   ] =        Sync_Enabled     .Encode();
        s_ssup[p_ON   ] = StartupSync_Enabled     .Encode();
        s_ssup[p_DELAY] = StartupSync_Delay       .Encode();
        s_miku[p_ON   ] =  Monitoring_Enabled     .Encode();
        s_miku[p_DELAY] =  Monitoring_Delay       .Encode();
        return result;
    }

    private static bool SetProperty
        (string section, string name, string value, int ln)
    {
        var b = false;
        switch (section)
        {
            case s_SERV when b = name == p_LOG_L: Server_MinLogLevel  = value.Parse_Int(ln, name, Server_MinLogLevel); break;
            case s_SERV when b = name == p_URLS:  Server_URLs         = value.Parse_StringArray(); break;
            case s_SYNC when b = name == p_ON:    Sync_Enabled        = value.Parse_Bool(); break;
            case s_SSUP when b = name == p_ON:    StartupSync_Enabled = value.Parse_Bool(); break;
            case s_SSUP when b = name == p_DELAY: StartupSync_Delay   = value.Parse_Time(ln, name, StartupSync_Delay); break;
            case s_MIKU when b = name == p_ON:    Monitoring_Enabled  = value.Parse_Bool(); break;
            case s_MIKU when b = name == p_DELAY: Monitoring_Delay    = value.Parse_Time(ln, name, Monitoring_Delay); break;
        }
        return b; // whether a property was matched
    }

    private static readonly SemaphoreSlim _s = new(1, 1);

    // FILE - READ / WRITE

    public static async Task ReadFrom_File() => await _s.Lock(async () =>
    {
        if (File_Config.FileExists.Janai())
        {
            await WriteTo_File();
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
            if (line.StartsWith('#'))       continue;
            if (line.StartsWith('['))
            {
                section = line.EndsWith(']') ? line[1..^1] : line[1..];
            }
            else if (line.Contains('='))
            {
                line.Split(bits, '=', TrimEntries);
                var name  = line[bits[0]];
                var value = line[bits[1]];
                if (SetProperty(section, name, value, ln).Failed())
                    LogWarn(NAME_R, WARN_UNKNOWN_LINE.Format(ln, line_raw));
            }
            else
                LogWarn(NAME_R, WARN_INVALID_PROP.Format(ln, line_raw));
        }
        ApplyConfig();
    });

    private static async Task WriteTo_File()
    {
        Dir_UserConfig.EnsureDirectoryExist();
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
            yield return $"[{section}]";
            foreach (var (name, value) in props)
            {
                yield return $"{name} = {value}";
            }
        }
    }

    // API - GET / UPDATE

    private static bool _restart_needed;

    public static async Task<API_Config>
        SendTo_API
        () => await _s.Lock(() => Tree);

    public static async Task<bool>
        ReceiveFrom_API
        (API_Config data) => await _s.Lock(async () =>
    {
        var prev_urls = Server_URLs;

        _tree = null; // invalidate cache

        foreach (var (section, props) in data)
        foreach (var (name,    value) in props)
        {
            if (SetProperty(section, name, value, 1).Failed())
                LogWarn(NAME_R, WARN_UNKNOWN_LINE.Format(1, $"{section} : {name}"));
        }
        ApplyConfig();

        await Task.WhenAll(WriteTo_File(), Miku.FollowConfig());

        if (_restart_needed
         || prev_urls is null != Server_URLs is null
         || prev_urls != null && Server_URLs != null
         && prev_urls.Order().SequenceEqual(Server_URLs.Order()))
            _restart_needed = true;

        if (_restart_needed)
            LogWarn(NAME_W, "RESTART NEEDED!", ConsoleColor.Red);

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

        LogWarn(NAME_R, WARN_INVALID_INT.Format(ln, name, s, fallback.Encode()));
        return fallback;
    }

    private static string Encode
        (this string[]? a) => a != null ? string.Join(';', a) : "";

    private static string[]? Parse_StringArray
        (this string s) => s == "" ? null : s.Split(';');

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
            LogWarn(NAME_R, WARN_INVALID_TIME.Format(ln, name, s, fallback.Encode()));
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
        N = "\n\e[47C\e[0m\e[3m", // prefix length + italics
        M = "\n\e[37C\e[92m",     // prefix length - "FALLBACK: " + CC_GREEN
        WARN_BASE = "Line {0,2} -> IGNORED ",
        WARN_INVALID_PROP = $"{WARN_BASE}(no '=' found)"     + $"{N}{{1}}"                             + $"\e[0m",
        WARN_UNKNOWN_LINE = $"{WARN_BASE}(unknown property)" + $"{N}{{1}}"                             + $"\e[0m",
        WARN_INVALID_INT  = $"{WARN_BASE}(invalid int32)"    + $"{N}{{1}} = {{2}}{M}FALLBACK: {{1}} = {{3}}\e[0m",
        WARN_INVALID_TIME = $"{WARN_BASE}(invalid time)"     + $"{N}{{1}} = {{2}}{M}FALLBACK: {{1}} = {{3}}\e[0m";
}

public class API_Config : Dictionary<string, Dictionary<string, string>>;
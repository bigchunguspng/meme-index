using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class Miku
{
    // ねえあたし知ってるよ　きみがひとり"XX"してるの知ってるよ
    // ビクンビクン震えてさ　声もダダ漏れなんだわ

    private const string NAME = "[Miku]";

    private static readonly SemaphoreSlim _s = new(1, 1);
    private static readonly Dictionary<string, FileSystemWatcher> _watchers = [];
    private static bool _monitoring;

    private static bool MikuShouldWork
        => Config.      Sync_Enabled
        && Config.Monitoring_Enabled;

    public static async Task Reload()
    {
        await Stop();
        await Start();
    }

    public static async Task FollowConfig()
    {
        if (MikuShouldWork) await Start();
        else                await Stop();
    }

    public static async Task Start() => await _s.Lock(async () =>
    {
        if (MikuShouldWork.Janai()) return;

        if (_monitoring) return;
        _ = _monitoring = true;

        await using var con = await AppDB.ConnectTo_Main();
        var monitors = await con.Monitors_GetAll(); // todo select only enabled
        var to_watch = monitors
            .Where(x => x.enabled)
            .GroupBy(x => x.path)
            .Select(g => (Path: g.Key, Recurse: g.Any(m => m.recurse)));

        foreach (var (path, recurse) in to_watch)
        {
            if (Directory.Exists(path))
            {
                StartWatching(path, recurse);
                Log(NAME, $"STARTED MONITORING {path}");
            }
            else
                LogWarn(NAME, $"DIRECTORY NOT FOUND {path}");
        }
    });

    public static async Task Stop() => await _s.Lock(() =>
    {
        if (_monitoring.Janai()) return;
        _ = _monitoring = false;

        foreach (var (path, watcher) in _watchers)
        {
            watcher.Dispose();
            Log(NAME, $"STOPPED MONITORING {path}");
        }
        _watchers.Clear();
    });

    private static void StartWatching(FilePath path, bool recurse)
    {
        var watcher = new FileSystemWatcher(path)
        {
            Filter = "*.*",
            IncludeSubdirectories = recurse,
            NotifyFilter
                = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite
                | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Created += HandleEvent;
        watcher.Changed += HandleEvent;
        watcher.Deleted += HandleEvent;
        watcher.Renamed += HandleEvent; // todo - rename file in db + send ui event (batched, 0.5s delay)
        watcher.Error   += HandleError;

        _watchers[path] = watcher;
    }

    private static void HandleEvent(object o, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.Name) ?? "*";
        var dir_or_image = Indexing.SupportedExtensions.Contains(extension);
        if (dir_or_image)
        {
            LogDebug(NAME, $"BUMP! {e.ChangeType}: {e.FullPath}");
            _ = DebounceAndTriggerSync();
        }
    }

    private static void HandleError(object o, ErrorEventArgs e)
    {
        var ex = e.GetException();
        if (ex is InternalBufferOverflowException)
        {
            LogWarn(NAME, $"BUMP! BUFFER OVERFLOW:\n{ex}");
            _ = DebounceAndTriggerSync();
        }
        else
        {
            LogError(NAME, $"ERROR:\n{ex}");
            App.LogException_JOB(ex, "Miku");
        }
    }

    private static volatile bool _waiting;

    private static async Task DebounceAndTriggerSync()
    {
        if (Interlocked.Exchange(ref _waiting, true))
        {
            // was waiting -> update time
            _waitSince = DateTime.UtcNow;
        }
        else
        {
            // wasn't waiting -> wait
            App.SetStatus("ABOUT TO SYNC");
            var waitSince  = _waitSince = DateTime.UtcNow;
            LogDebug(NAME, $"Sync in {WaitDelay.ReadableTime()}");
            await Task.Delay (WaitDelay);
            while
               (waitSince != _waitSince) // was updated -> wait more
            {
                waitSince  = _waitSince;
                var timeSince_waitSince = DateTime.UtcNow - _waitSince;
                var delay  =  WaitDelay - timeSince_waitSince;
                if (delay > TimeSpan.Zero)
                {
                    LogDebug(NAME, $"Sync in {delay.ReadableTime()}");
                    await Task.Delay(delay);
                }
            }
            _waitSince = default;
            _waiting = false;

            if (_monitoring)
            {
                Log(NAME, "Sync!");
                await Indexing.TriggerSync();
            }
        }
    }

    private static DateTime _waitSince;
    private static TimeSpan  WaitDelay
        => Config.Monitoring_Delay.TotalMilliseconds > 250
         ? Config.Monitoring_Delay
         : TimeSpan.FromMilliseconds(250);
}
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class Miku
{
    // ねえあたし知ってるよ　きみがひとり"XX"してるの知ってるよ
    // ビクンビクン震えてさ　声もダダ漏れなんだわ

    private static readonly Dictionary<string, FileSystemWatcher> _watchers = [];
    private static bool _monitoring;

    public static async Task Reload()
    {
        Stop();
        await Start();
    }

    [MethodImpl(Synchronized)]
    public static async Task Start()
    {
        var monitoring = Interlocked.Exchange(ref _monitoring, true);
        if (monitoring) return;

        _monitoring = true;
        await using var con = await AppDB.ConnectTo_Main();
        var monitors = await con.Monitors_GetAll();

        var to_watch = monitors
            .Where(x => x.enabled)
            .GroupBy(x => x.path)
            .Select(g => (Path: g.Key, Recurse: g.Any(m => m.recurse)));

        foreach (var (path, recurse) in to_watch)
        {
            StartWatching(path, recurse);
            Log($"Miku >> STARTED MONITORING {path}");
        }
    }

    [MethodImpl(Synchronized)]
    public static void Stop()
    {
        var monitoring = Interlocked.Exchange(ref _monitoring, false);
        if (monitoring.Janai()) return;

        foreach (var (path, watcher) in _watchers)
        {
            watcher.Dispose();
            Log($"Miku >> STOPPED MONITORING {path}");
        }
        _watchers.Clear();
    }

    private static void StartWatching(FilePath path, bool recurse)
    {
        var watcher = new FileSystemWatcher(path)
        {
            Filter = "*.*",
            IncludeSubdirectories = recurse,
            NotifyFilter
                = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.Size
                | NotifyFilters.LastWrite
                | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) => HandleEvent(e);
        watcher.Changed += (_, e) => HandleEvent(e);
        watcher.Deleted += (_, e) => HandleEvent(e);
        watcher.Renamed += (_, e) => HandleEvent(e); // todo - just rename file in db
        watcher.Error   += (_, e) => HandleError(e.GetException());

        _watchers[path] = watcher;
    }

    private static void HandleEvent(FileSystemEventArgs e)
    {
        if (Indexing.SupportedExtensions.Contains(e.Name.GetExtension_Or("*")))
            Bump();
    }

    private static void HandleError(Exception e)
    {
        if (e is InternalBufferOverflowException)
        {
            LogWarn($"Miku >> BUFFER OVERFLOW:\n{e}");
            Bump();
        }
        else
        {
            LogWarn($"Miku >> ERROR:\n{e}");
            App.LogException_JOB(e, "Miku");
        }
    }

    private static void Bump()
    {
        _ = DebounceAndTriggerSync();
    }

    private static async Task DebounceAndTriggerSync()
    {
        if (_waitSince != default) // already waiting
        {
            _waitSince = DateTime.UtcNow;
        }
        else // not waiting
        {
            var waitSince  = _waitSince = DateTime.UtcNow;
            await Task.Delay(_waitDelay);
            while 
               (waitSince != _waitSince) // was updated -> wait more
            {
                waitSince  = _waitSince;
                var timeSince_waitSince = DateTime.UtcNow - _waitSince;
                var delay  = _waitDelay - timeSince_waitSince;
                await Task.Delay(delay);
            }
            _waitSince = default;
            await Indexing.TriggerSync();
        }
    }

    private static TimeSpan _waitDelay = TimeSpan.FromSeconds(5);
    private static DateTime _waitSince;
}

// TO TEST:
// what if I move files from disabled monitor to enabled one?
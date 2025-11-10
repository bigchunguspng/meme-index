namespace MemeIndex_Core.Services.Indexing;

/// Detects changes in specific directory in real time.
public class DirectoryMonitor(FileWatchService service, string path, bool recursive)
{
    private readonly FileSystemWatcher _watcher = new (path)
    {
        IncludeSubdirectories = recursive
    };

    public string Path => _watcher.Path;

    public bool Recursive
    {
        get => _watcher.IncludeSubdirectories;
        set => _watcher.IncludeSubdirectories = value;
    }

    public void Start()
    {
        _watcher.NotifyFilter = NotifyFilters.FileName
                              | NotifyFilters.DirectoryName
                              | NotifyFilters.LastWrite
                              | NotifyFilters.CreationTime;
        _watcher.Filter = "*.*";
        _watcher.Created += service.OnCreated;
        _watcher.Changed += service.OnChanged;
        _watcher.Renamed += service.OnRenamed;
        _watcher.Deleted += service.OnDeleted;
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.Created -= service.OnCreated;
        _watcher.Changed -= service.OnChanged;
        _watcher.Renamed -= service.OnRenamed;
        _watcher.Deleted -= service.OnDeleted;
        _watcher.Dispose();
    }
}
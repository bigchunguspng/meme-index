namespace MemeIndex_Core.Services.Indexing;

/// <summary>
/// Detects changes in specific directory in real time.
/// </summary>
public class DirectoryMonitor
{
    private readonly FileWatchService _service;
    private readonly FileSystemWatcher _watcher;

    public string Path => _watcher.Path;

    public bool Recursive
    {
        get => _watcher.IncludeSubdirectories;
        set => _watcher.IncludeSubdirectories = value;
    }

    public DirectoryMonitor(FileWatchService service, string path, bool recursive)
    {
        _service = service;
        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = recursive
        };
    }

    public void Start()
    {
        _watcher.NotifyFilter = NotifyFilters.FileName
                              | NotifyFilters.DirectoryName
                              | NotifyFilters.LastWrite
                              | NotifyFilters.CreationTime;
        _watcher.Filter = "*.*";
        _watcher.Created += _service.OnCreated;
        _watcher.Changed += _service.OnChanged;
        _watcher.Renamed += _service.OnRenamed;
        _watcher.Deleted += _service.OnDeleted;
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        _watcher.Created -= _service.OnCreated;
        _watcher.Changed -= _service.OnChanged;
        _watcher.Renamed -= _service.OnRenamed;
        _watcher.Deleted -= _service.OnDeleted;
        _watcher.Dispose();
    }
}
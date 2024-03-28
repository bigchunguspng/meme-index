using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Services;

public class FileWatchService
{
    private readonly IDirectoryService _directoryService;
    private readonly List<FileSystemWatcher> _watchers;

    // created for tracked folders on startup
    // added new when adding folder to tracking list

    public FileWatchService(IDirectoryService directoryService)
    {
        _directoryService = directoryService;
        _watchers = new List<FileSystemWatcher>();
    }

    #region WATCHING

    public void Start()
    {
        var watchers = _directoryService
            .GetTracked()
            .Select(x => x.Path)
            .Where(Directory.Exists)
            .Select(x => new FileSystemWatcher(x));
        _watchers.AddRange(watchers);

        foreach (var watcher in _watchers) Start(watcher);
    }

    public void Stop()
    {
        foreach (var watcher in _watchers) Stop(watcher);
    }

    public void AddDirectory(string path)
    {
        var watcher = new FileSystemWatcher(path);
        _watchers.Add(watcher);
        Start(watcher);
    }

    public void RemoveDirectory(string path)
    {
        var watcher = _watchers.First(x => x.Path == path);
        _watchers.Remove(watcher);
        Stop(watcher);
    }

    private void Start(FileSystemWatcher watcher)
    {
        watcher.NotifyFilter = NotifyFilters.FileName
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.LastWrite
                               | NotifyFilters.CreationTime;
        watcher.Filter = "*.*";
        watcher.IncludeSubdirectories = true;
        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.Deleted += OnDeleted;
        watcher.EnableRaisingEvents = true;
    }

    private void Stop(FileSystemWatcher watcher)
    {
        watcher.Created -= OnCreated;
        watcher.Changed -= OnChanged;
        watcher.Renamed -= OnRenamed;
        watcher.Deleted -= OnDeleted;
        watcher.Dispose();
    }

    #endregion


    #region EVENTS

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        // add to db, ocr
        Logger.Log("Created", ConsoleColor.Yellow);
        if (e.FullPath.IsFile())
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                // index
                // process
            }
        }
        else if (e.FullPath.IsDirectory())
        {
            // index
            // index and process files
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        // update db entry
        Logger.Log("Renamed", ConsoleColor.Yellow);
        if (e.FullPath.IsFile())
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                // update file
            }
        }
        else if (e.FullPath.IsDirectory())
        {
            _directoryService.Update(e.OldFullPath, e.FullPath);
        }
    }

    private void OnChanged(object source, FileSystemEventArgs e)
    {
        // update db entry, size differs => ocr
        Logger.Log("Changed", ConsoleColor.Yellow);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        // delete from db
        Logger.Log("Deleted", ConsoleColor.Yellow);
    }

    #endregion
}
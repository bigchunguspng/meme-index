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

    public void StartAll()
    {
        var directories = _directoryService.GetTracked().Where(x => Directory.Exists(x.Path));
        _watchers.AddRange(directories.Select(x => new FileSystemWatcher(x.Path)));
        foreach (var watcher in _watchers) Start(watcher);
    }

    public void DisposeAll()
    {
        foreach (var watcher in _watchers) Dispose(watcher);
    }

    private void Start(FileSystemWatcher watcher)
    {
        watcher.NotifyFilter = NotifyFilters.FileName
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.LastWrite
                               | NotifyFilters.CreationTime;
        watcher.Filter = "*.*";
        watcher.IncludeSubdirectories = true;
        watcher.Created += OnCreated; // - new file spotted
        watcher.Changed += OnChanged; // - file change spotted
        watcher.Renamed += OnRenamed; // - file rename spotted
        watcher.Deleted += OnDeleted; // - file deletion spotted
        watcher.EnableRaisingEvents = true;
    }

    private void Dispose(FileSystemWatcher watcher)
    {
        watcher.Created -= OnCreated;
        watcher.Changed -= OnChanged;
        watcher.Renamed -= OnRenamed;
        watcher.Deleted -= OnDeleted;
        watcher.Dispose();
    }

    public async Task AddDirectory(string path)
    {
        if (IsDirectory(path))
        {
            await _directoryService.Add(path);

            var watcher = new FileSystemWatcher(path);
            _watchers.Add(watcher);
            Start(watcher);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);
        }
    }

    public async Task RemoveDirectory(string path)
    {
        if (IsDirectory(path))
        {
            await _directoryService.Remove(path);

            var watcher = _watchers.First(x => x.Path == path);
            _watchers.Remove(watcher);
            Dispose(watcher);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] removed", path);
        }
    }

    // can be file or folder

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        // add to db, ocr
        Logger.Log("Created", ConsoleColor.Yellow);
        if (IsFile(e.FullPath))
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                // index
                // process
            }
        }
        else if (IsDirectory(e.FullPath))
        {
            // index
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        // update db entry
        Logger.Log("Renamed", ConsoleColor.Yellow);
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

    private bool IsFile(string path) => File.Exists(path);
    private bool IsDirectory(string path) => Directory.Exists(path);
}
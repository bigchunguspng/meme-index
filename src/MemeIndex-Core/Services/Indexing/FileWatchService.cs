using MemeIndex_Core.Data;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Services.Indexing;

public class FileWatchService
{
    private readonly IDirectoryService _directoryService;
    private readonly IFileService _fileService;
    private readonly IMonitoringService _monitoringService;
    private readonly MemeDbContext _context;
    private readonly List<DirectoryMonitor> _watchers;
    private readonly List<FileChange> _fileChanges;

    public FileWatchService(IDirectoryService directoryService, IFileService fileService, IMonitoringService monitoringService, MemeDbContext context)
    {
        _directoryService = directoryService;
        _fileService = fileService;
        _monitoringService = monitoringService;
        _context = context;
        _watchers = new List<DirectoryMonitor>();
        _fileChanges = new List<FileChange>();
    }

    #region WATCHING

    public async Task Start()
    {
        var monitored = await _monitoringService.GetDirectories();
        var watchers = monitored
            .Where(x => Directory.Exists(x.Directory.Path))
            .Select(x => new DirectoryMonitor(this, x.Directory.Path, x.Recursive));
        _watchers.AddRange(watchers);

        foreach (var watcher in _watchers) watcher.Start();
    }

    public void Stop()
    {
        foreach (var watcher in _watchers) watcher.Stop();
    }

    public void AddDirectory(string path, bool recursive)
    {
        var watcher = new DirectoryMonitor(this, path, recursive);
        _watchers.Add(watcher);
        watcher.Start();
    }

    public void RemoveDirectory(string path)
    {
        var watcher = _watchers.First(x => x.Path == path);
        _watchers.Remove(watcher);
        watcher.Stop();
    }

    #endregion


    #region EVENTS

    public void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.IsFile())
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                RegisterEvent(e);
            }
        }
        else if (e.FullPath.IsDirectory())
        {
            // created directory can contain files
            RegisterEvent(e);
        }

        Logger.Log("Created", ConsoleColor.Yellow);
    }

    public async void OnRenamed(object sender, RenamedEventArgs e)
    {
        // rename is handled instantly

        if (e.FullPath.IsFile())
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                var entity = await _context.Files.FirstOrDefaultAsync
                (
                    x => x.Name == e.OldName && x.Size == file.Length && x.Modified == file.LastWriteTime
                );
                if (entity != null)
                {
                    await _fileService.UpdateFile(entity, file);
                }
            }
        }
        else if (e.FullPath.IsDirectory())
        {
            await _directoryService.Update(e.OldFullPath, e.FullPath);
        }

        Logger.Log("Renamed", ConsoleColor.Yellow);
    }

    public void OnChanged(object source, FileSystemEventArgs e)
    {
        if (e.FullPath.IsDirectory()) return;

        if (e.FullPath.IsFile())
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                RegisterEvent(e);
            }
        }

        Logger.Log("Changed", ConsoleColor.Yellow);
    }

    public void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.IsFile())
        {
            var file = new FileInfo(e.FullPath);
            if (file.IsImage())
            {
                RegisterEvent(e);
            }
        }
        else if (e.FullPath.IsDirectory())
        {
            // deleted directory can contain files
            RegisterEvent(e);
        }

        Logger.Log("Deleted", ConsoleColor.Yellow);
    }

    private void RegisterEvent(FileSystemEventArgs e)
    {
        _fileChanges.Add(new FileChange(e.FullPath, e.ChangeType));
    }
    // after registration:
    // wait for 1 second (debounce)
    // if no new was added >> interpret events >> process
    
    
    
    // file copied >> created + 3-4 x changed
    // file moved  >> deleted created
    // file edited with Paint >>    6 x changed
    // file edited with Pinta >> ~100 x changed
    // folder deleted >> 1 deleted
    // folder shift+deleted >> N x deleted (for each file)
    
    // changed -> update db entry, size differs => ocr
    // deleted -> delete from db

    #endregion
}

public record FileChange(string Path, WatcherChangeTypes Type);
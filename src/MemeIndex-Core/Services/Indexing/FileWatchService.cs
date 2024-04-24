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
    private readonly DebounceTimer _debounce;

    public FileWatchService(IDirectoryService directoryService, IFileService fileService, IMonitoringService monitoringService, MemeDbContext context)
    {
        _directoryService = directoryService;
        _fileService = fileService;
        _monitoringService = monitoringService;
        _context = context;
        _watchers = new List<DirectoryMonitor>();
        _debounce = new DebounceTimer();
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

    public void StartWatching(string path, bool recursive)
    {
        var watcher = new DirectoryMonitor(this, path, recursive);
        _watchers.Add(watcher);
        watcher.Start();
    }

    public void StopWatching(string path)
    {
        var watcher = _watchers.First(x => x.Path == path);
        _watchers.Remove(watcher);
        watcher.Stop();
    }

    public void ChangeRecursion(string path, bool recursive)
    {
        var watcher = _watchers.FirstOrDefault(x => x.Path == path);
        if (watcher is not null)
        {
            watcher.Recursive = recursive;
        }
    }

    #endregion


    #region EVENTS

    public void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.DirectoryExists() || e.FullPath.FileExists() && new FileInfo(e.FullPath).IsImage())
        {
            RegisterEvent();
            Logger.Log("Created", ConsoleColor.Yellow);
        }
    }

    public async void OnRenamed(object sender, RenamedEventArgs e)
    {
        // rename is handled instantly

        if (e.FullPath.FileExists())
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
                    Logger.Log("Renamed file", ConsoleColor.Yellow);
                }
            }
        }
        else if (e.FullPath.DirectoryExists())
        {
            await _directoryService.Update(e.OldFullPath, e.FullPath);
            Logger.Log("Renamed directory", ConsoleColor.Yellow);
        }
    }

    public void OnChanged(object source, FileSystemEventArgs e)
    {
        if (e.FullPath.DirectoryExists()) return;

        if (e.FullPath.FileExists() && new FileInfo(e.FullPath).IsImage())
        {
            RegisterEvent();
            Logger.Log("Changed", ConsoleColor.Yellow);
        }
    }

    public void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (Path.GetExtension(e.FullPath).IsImageFileExtension() == false)
        {
            return;
        }

        RegisterEvent();
        Logger.Log("Deleted", ConsoleColor.Yellow);
    }

    private async void RegisterEvent()
    {
        var b = await _debounce.Wait(milliseconds: 4000);
        if (b) await UpdateFileSystemKnowledge?.Invoke()!;
    }

    public event AsyncEventHandler? UpdateFileSystemKnowledge;

    public delegate Task AsyncEventHandler();

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
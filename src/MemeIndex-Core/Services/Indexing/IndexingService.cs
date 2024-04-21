using MemeIndex_Core.Entities;
using MemeIndex_Core.Model;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Utils;
using Directory = System.IO.Directory;

namespace MemeIndex_Core.Services.Indexing;

public class IndexingService
{
    private readonly FileWatchService _watch;
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;
    private readonly IMonitoringService _monitoringService;
    private readonly OvertakingService _overtakingService;

    public IndexingService
    (
        FileWatchService watch,
        IFileService fileService,
        IDirectoryService directoryService,
        IMonitoringService monitoringService,
        OvertakingService overtakingService
    )
    {
        _watch = watch;
        _fileService = fileService;
        _directoryService = directoryService;
        _monitoringService = monitoringService;
        _overtakingService = overtakingService;
    }

    public Task<List<MonitoredDirectory>> GetTrackedDirectories()
    {
        return _monitoringService.GetDirectories();
    }

    public async Task AddDirectory(MonitoringOptions options)
    {
        var path = options.Path;
        if (path.DirectoryExists())
        {
            Logger.Status($"Adding \"{path}\"...");

            // add to db, start watching
            await _monitoringService.AddDirectory(options);
            _watch.AddDirectory(path, options.Recursive);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);

            // add to db all files
            var files = Helpers.GetImageFiles(path, options.Recursive);

            Logger.Log(ConsoleColor.Magenta, "Files: {0}", files.Count);

            var tasks = files.Select(file => _fileService.AddFile(file));
            await Task.WhenAll(tasks);

            Logger.Log("Done", ConsoleColor.Magenta);
        }
    }

    /// <summary>
    /// This method should be called intentionally by user,
    /// because it removes all files from that directory from index.
    /// </summary>
    public async Task RemoveDirectory(string path)
    {
        // remove from db
        // stop watching

        if (path.DirectoryExists())
        {
            Logger.Status($"Removing \"{path}\"...");

            await _monitoringService.RemoveDirectory(path);
            _watch.RemoveDirectory(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] removed", path);
        }
    }

    public async Task<IEnumerable<MonitoredDirectory>> GetMissingDirectories()
    {
        var monitored = await _monitoringService.GetDirectories();
        return monitored.Where(x => !Directory.Exists(x.Directory.Path));
    }

    public void MoveDirectory(string oldPath, string newPath)
    {
        if (newPath.DirectoryExists())
        {
            _directoryService.Update(oldPath, newPath);
        }
    }

    private bool FileWasUpdated(FileInfo fileInfo, Entities.File entity)
    {
        // (so it needs reindexing by visual means)
        return fileInfo.Length != entity.Size ||
               fileInfo.LastWriteTimeUtc > entity.Tracked ||
               fileInfo.CreationTimeUtc  > entity.Tracked;
    }

    public async void StartIndexingAsync()
    {
        // todo ask to locate missing dirs

        await _overtakingService.OvertakeMissingFiles();

        // todo check all files for changes with FileWasUpdated()

        await _watch.Start();
    }

    public void StopIndexing()
    {
        _watch.Stop();
    }
}
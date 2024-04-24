using MemeIndex_Core.Model;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Controllers;

public class IndexController
{
    private readonly IMonitoringService _monitoringService;
    private readonly IFileService _fileService;
    private readonly IndexingService _indexingService;
    private readonly FileWatchService _fileWatchService;
    private readonly OvertakingService _overtakingService;

    public IndexController
    (
        IMonitoringService monitoringService,
        IFileService fileService,
        IndexingService indexingService,
        FileWatchService fileWatchService,
        OvertakingService overtakingService
    )
    {
        _monitoringService = monitoringService;
        _fileService = fileService;
        _indexingService = indexingService;
        _fileWatchService = fileWatchService;
        _overtakingService = overtakingService;

        _fileWatchService.UpdateFileSystemKnowledge += UpdateFileSystemKnowledge;
    }

    public async Task<IEnumerable<MonitoringOptions>> GetMonitoringOptions()
    {
        var directories = await _monitoringService.GetDirectories();
        return directories.Select(x =>
        {
            var means = x.IndexingOptions.Select(o => o.MeanId).Distinct().ToHashSet();
            return new MonitoringOptions(x.Directory.Path, x.Recursive, means);
        });
    }

    public Task UpdateMonitoringDirectories(IEnumerable<MonitoringOptions> options)
    {
        throw new NotImplementedException();
    }

    public async Task AddDirectory(MonitoringOptions options)
    {
        var directory = await _monitoringService.AddDirectory(options);
        await _fileService.AddFiles(directory);
        _fileWatchService.StartWatching(options.Path, options.Recursive);
        await _indexingService.ProcessPendingFiles();

        Logger.Status($"Added {options.Path}");
    }

    public async Task UpdateDirectory(MonitoringOptions options)
    {
        var changed = await _monitoringService.UpdateDirectory(options);
        if (changed)
        {
            _fileWatchService.ChangeRecursion(options.Path, options.Recursive);
            await UpdateFileSystemKnowledge();

            Logger.Status($"Updated {options.Path}");
        }
    }

    public async Task RemoveDirectory(string path)
    {
        _fileWatchService.StopWatching(path);
        await _monitoringService.RemoveDirectory(path);

        Logger.Status($"Removed {path}");
    }

    public async Task UpdateFileSystemKnowledge()
    {
        var files = await _overtakingService.UpdateFileSystemKnowledge();
        if (files > 0) await _indexingService.ProcessPendingFiles();
    }

    public async void StartIndexing()
    {
        // todo ask to locate missing dirs (another method, called by ui before this one)

        await UpdateFileSystemKnowledge();
        await _fileWatchService.Start();
    }

    public void StopIndexing()
    {
        _fileWatchService.Stop();
    }
}

/*
ms  monitoringService
fws fileWatchService
is  indexingService
*/
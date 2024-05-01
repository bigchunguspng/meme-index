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

    public async Task<List<MonitoringOptions>> GetMonitoringOptions()
    {
        var directories = await _monitoringService.GetDirectories();
        return directories.Select(x =>
        {
            var means = x.IndexingOptions.Select(o => o.MeanId).Distinct().ToHashSet();
            return new MonitoringOptions(x.Directory.Path, x.Recursive, means);
        }).ToList();
    }

    public async Task UpdateMonitoringDirectories(IEnumerable<MonitoringOptions> options)
    {
        var optionsDb = await GetMonitoringOptions();
        var optionsMf = options.ToList();

        var directoriesDb = optionsDb.Select(x => x.Path).ToList();
        var directoriesMf = optionsMf.Select(x => x.Path).ToList();

        var add = directoriesMf.Except(directoriesDb).OrderBy          (x => x.Length).ToList();
        var rem = directoriesDb.Except(directoriesMf).OrderByDescending(x => x.Length).ToList();
        var upd = directoriesDb.Intersect(directoriesMf).Where(path =>
        {
            var db = optionsDb.First(op => op.Path == path);
            var mf = optionsMf.First(op => op.Path == path);
            return !db.IsTheSameAs(mf);
        }).ToList();

        foreach (var path in rem)
        {
            await RemoveDirectory(path);
        }

        foreach (var option in add.Select(path => optionsMf.First(x => x.Path == path)))
        {
            await AddDirectory(option);
        }

        foreach (var option in upd.Select(path => optionsMf.First(x => x.Path == path)))
        {
            await UpdateDirectory(option);
        }
    }

    public async Task AddDirectory(MonitoringOptions options)
    {
        var directory = await _monitoringService.AddDirectory(options);
        await _fileService.AddFiles(directory);
        _fileWatchService.StartWatching(options.Path, options.Recursive);

        Logger.Status($"Added {options.Path.Quote()}");
    }

    public async Task UpdateDirectory(MonitoringOptions options)
    {
        var changed = await _monitoringService.UpdateDirectory(options);
        if (changed)
        {
            _fileWatchService.ChangeRecursion(options.Path, options.Recursive);

            Logger.Status($"Updated {options.Path.Quote()}");
        }
    }

    public async Task RemoveDirectory(string path)
    {
        _fileWatchService.StopWatching(path);
        await _monitoringService.RemoveDirectory(path);

        Logger.Status($"Removed {path.Quote()}");
    }

    public async Task UpdateFileSystemKnowledge()
    {
        await _overtakingService.UpdateFileSystemKnowledge();
        await _indexingService.ProcessPendingFiles();
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
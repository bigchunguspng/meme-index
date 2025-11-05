using MemeIndex_Core.Data;
using MemeIndex_Core.Objects;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.Data.Contracts;
using MemeIndex_Core.Services.Indexing;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Controllers;

public class IndexController
{
    private readonly MemeDbContext _context;
    private readonly IMonitoringService _monitoringService;
    private readonly IFileService _fileService;
    private readonly TagService _tagService;
    private readonly IndexingService _indexingService;
    private readonly FileWatchService _fileWatchService;
    private readonly OvertakingService _overtakingService;

    public IndexController
    (
        MemeDbContext context,
        IMonitoringService monitoringService,
        IFileService fileService,
        IndexingService indexingService,
        FileWatchService fileWatchService,
        OvertakingService overtakingService,
        TagService tagService
    )
    {
        _context = context;
        _monitoringService = monitoringService;
        _fileService = fileService;
        _indexingService = indexingService;
        _fileWatchService = fileWatchService;
        _overtakingService = overtakingService;
        _tagService = tagService;

        _fileWatchService.UpdateFileSystemKnowledge += UpdateFileSystemKnowledgeSafe;
    }

    public async Task<List<MonitoringOption>> GetMonitoringOptions()
    {
        var directories = await _monitoringService.GetDirectories();
        return directories.Select(x =>
        {
            var means = x.IndexingOptions.Select(o => o.MeanId).Distinct().ToHashSet();
            return new MonitoringOption(x.Directory.Path, x.Recursive, means);
        }).ToList();
    }

    public async Task UpdateMonitoringDirectories(IEnumerable<MonitoringOption> options)
    {
        var optionsDb = await GetMonitoringOptions();
        var optionsMf = options.ToList();

        var directoriesDb = optionsDb.Select(x => x.Path).ToList();
        var directoriesMf = optionsMf.Select(x => x.Path).ToList();

        var add = directoriesMf.Except(directoriesDb).OrderBy          (x => x.Length).ToList();
        var rem = directoriesDb.Except(directoriesMf).OrderByDescending(x => x.Length).ToList();
        var upd = directoriesDb.Intersect(directoriesMf).Where(path =>
        {
            var dbOption = optionsDb.First(op => op.Path == path);
            var mfOption = optionsMf.First(op => op.Path == path);
            return !dbOption.IsTheSameAs(mfOption);
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

    public async Task AddDirectory(MonitoringOption option)
    {
        var directory = await _monitoringService.AddDirectory(option);
        await _fileService.AddFiles(directory);
        _fileWatchService.StartWatching(option.Path, option.Recursive);

        Logger.Status($"Added {option.Path.Quote()}");
    }

    public async Task UpdateDirectory(MonitoringOption option)
    {
        var changed = await _monitoringService.UpdateDirectory(option);
        if (changed)
        {
            _fileWatchService.ChangeRecursion(option.Path, option.Recursive);

            Logger.Status($"Updated {option.Path.Quote()}");
        }
    }

    public async Task RemoveDirectory(string path)
    {
        _fileWatchService.StopWatching(path);
        await _monitoringService.RemoveDirectory(path);

        Logger.Status($"Removed {path.Quote()}");
    }

    public Task<int> RemoveTagsByMean(int meanId)
    {
        return _tagService.RemoveTagsByMean(meanId);
    }

    public async Task UpdateFileSystemKnowledgeSafe()
    {
        await _context.Access.WaitAsync();
        await UpdateFileSystemKnowledge();
        _context.Access.Release();
    }

    private async Task UpdateFileSystemKnowledge()
    {
        await _overtakingService.UpdateFileSystemKnowledge();
        await _indexingService.ProcessPendingFiles();
    }

    public async void StartIndexing()
    {
        // todo ask to locate missing dirs (another method, called by ui before this one)

        await _fileWatchService.Start();
        await UpdateFileSystemKnowledge();
    }

    public void StopIndexing()
    {
        _fileWatchService.Stop();
    }
}
using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Objects;
using MemeIndex_Core.Services.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Data.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public class MonitoringService : IMonitoringService
{
    private readonly MemeDbContext _context;

    public MonitoringService(MemeDbContext context)
    {
        _context = context;
    }

    public Task<List<MonitoredDirectory>> GetDirectories()
    {
        return _context.MonitoredDirectories
            .Include(x => x.Directory)
            .Include(x => x.IndexingOptions)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    public IQueryable<MonitoredDirectory> GetDirectories(int meanId)
    {
        return _context.MonitoredDirectories
            .Include(x => x.Directory)
            .Include(x => x.IndexingOptions)
            .AsSplitQuery()
            .Where(x => x.IndexingOptions.Any(io => io.MeanId == meanId));
    }

    public async Task<MonitoredDirectory> AddDirectory(MonitoringOption option)
    {
        // todo handle recursion and nested directories
        // if this directory is inside of one that is monitored recursively >>
        // files should be tracked BY THE ONE WITH SHORTER PATH

        var directory = await GetDirectoryByPath(option.Path) ?? await AddDirectory(option.Path);

        var monitored = new MonitoredDirectory
        {
            DirectoryId = directory.Id,
            Recursive = option.Recursive
        };
        await _context.MonitoredDirectories.AddAsync(monitored);
        await _context.SaveChangesAsync();

        foreach (var mean in option.Means)
        {
            var indexingOption = new IndexingOption
            {
                MonitoredDirectoryId = monitored.Id,
                MeanId = mean
            };
            await _context.IndexingOptions.AddAsync(indexingOption);
        }

        await _context.SaveChangesAsync();

        return monitored;
    }

    public async Task RemoveDirectory(string path)
    {
        var directory = await GetDirectoryByPath(path);
        if (directory is null) return;

        var monitored = await _context.MonitoredDirectories.FirstOrDefaultAsync(x => x.DirectoryId == directory.Id);
        if (monitored is null) return;

        if (monitored.Recursive) _context.Directories.RemoveRange(GetDirectoryBranch(directory.Path));
        else /*               */ _context.Directories.Remove(directory);

        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateDirectory(MonitoringOption option)
    {
        var directory = await GetDirectoryByPath(option.Path);
        if (directory is null) return false;

        var monitored = await _context.MonitoredDirectories.FirstOrDefaultAsync(x => x.DirectoryId == directory.Id);
        if (monitored is null) return false;

        var changeRecursion = monitored.Recursive != option.Recursive;
        if (changeRecursion)
        {
            monitored.Recursive = option.Recursive;
            await _context.SaveChangesAsync();
        }

        var means = _context.IndexingOptions
            .Where(x => x.MonitoredDirectoryId == monitored.Id)
            .Select(x => x.MeanId)
            .ToHashSet();

        if (means.SetEquals(option.Means) == false)
        {
            foreach (var mean in option.Means.Where(x => !means.Contains(x)))
            {
                // ADD
                var io = new IndexingOption { MonitoredDirectoryId = monitored.Id, MeanId = mean };
                await _context.IndexingOptions.AddAsync(io);
            }

            foreach (var mean in means.Where(x => !option.Means.Contains(x)))
            {
                // REMOVE
                var io = await _context.IndexingOptions
                    .FirstOrDefaultAsync(x => x.MonitoredDirectoryId == monitored.Id && x.MeanId == mean);
                if (io is not null) _context.IndexingOptions.Remove(io);
            }

            await _context.SaveChangesAsync();
        }

        return changeRecursion;
    }


    private Task<Directory?> GetDirectoryByPath(string path)
    {
        return _context.Directories.FirstOrDefaultAsync(x => x.Path == path);
    }

    private async Task<Directory> AddDirectory(string path)
    {
        var directory = new Directory { Path = path };
        await _context.Directories.AddAsync(directory);
        await _context.SaveChangesAsync();
        return directory;
    }

    private IEnumerable<Directory> GetDirectoryBranch(string path)
    {
        return _context.Directories.Where(x => x.Path.StartsWith(path));
    }

    private Task<MonitoredDirectory?> GetOuterRecursivelyMonitoredDirectory(Directory directory)
    {
        return _context.MonitoredDirectories
            .Include(x => x.Directory)
            .Where(x => x.Recursive && x.DirectoryId != directory.Id)
            .OrderBy(x => x.Directory.Path.Length)
            .FirstOrDefaultAsync(x => directory.Path.StartsWith(x.Directory.Path));
    }

    private async Task<List<MonitoredDirectory>> GetInnerMonitoredDirectories(Directory directory)
    {
        var directories = await _context.MonitoredDirectories
            .Include(x => x.Directory)
            .Where(x => x.DirectoryId != directory.Id)
            .Where(x => x.Directory.Path.StartsWith(directory.Path))
            .ToListAsync();

        return directories
            .Where(x => !directories.Any(y =>
            {
                var pathX = x.Directory.Path;
                var pathY = y.Directory.Path;
                return y.Recursive && pathY.Length < pathX.Length && pathX.StartsWith(pathY);
            }))
            .ToList();
    }
}
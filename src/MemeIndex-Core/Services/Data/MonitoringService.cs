using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Objects;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Data.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public class MonitoringService(MemeDbContext context)
{
    /// Returns a list of monitored directories, including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties.
    public Task<List<MonitoredDirectory>> GetDirectories()
    {
        return context.MonitoredDirectories
            .Include(x => x.Directory)
            .Include(x => x.IndexingOptions)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    /// Returns a list of monitored directories, [including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties],
    /// which should be indexed by a <see cref="Mean"/> with given id.
    public IQueryable<MonitoredDirectory> GetDirectories(int meanId)
    {
        return context.MonitoredDirectories
            .Include(x => x.Directory)
            .Include(x => x.IndexingOptions)
            .AsSplitQuery()
            .Where(x => x.IndexingOptions.Any(io => io.MeanId == meanId));
    }

    /// Adds a directory to monitoring list according to provided options.
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
        await context.MonitoredDirectories.AddAsync(monitored);
        await context.SaveChangesAsync();

        foreach (var mean in option.Means)
        {
            var indexingOption = new IndexingOption
            {
                MonitoredDirectoryId = monitored.Id,
                MeanId = mean
            };
            await context.IndexingOptions.AddAsync(indexingOption);
        }

        await context.SaveChangesAsync();

        return monitored;
    }

    /// Removes directory from monitoring list.
    /// It also removes all of its files if the directory isn't located
    /// inside of another directory, that is <b>recursively</b> monitored.
    public async Task RemoveDirectory(string path)
    {
        var directory = await GetDirectoryByPath(path);
        if (directory is null) return;

        var monitored = await context.MonitoredDirectories.FirstOrDefaultAsync(x => x.DirectoryId == directory.Id);
        if (monitored is null) return;

        if (monitored.Recursive) context.Directories.RemoveRange(GetDirectoryBranch(directory.Path));
        else /*               */ context.Directories.Remove(directory);

        await context.SaveChangesAsync();
    }

    /// Updates monitoring options of the directory.
    /// <returns> The value indicating whether the recursion option was altered. </returns>
    public async Task<bool> UpdateDirectory(MonitoringOption option)
    {
        var directory = await GetDirectoryByPath(option.Path);
        if (directory is null) return false;

        var monitored = await context.MonitoredDirectories.FirstOrDefaultAsync(x => x.DirectoryId == directory.Id);
        if (monitored is null) return false;

        var changeRecursion = monitored.Recursive != option.Recursive;
        if (changeRecursion)
        {
            monitored.Recursive = option.Recursive;
            await context.SaveChangesAsync();
        }

        var means = context.IndexingOptions
            .Where(x => x.MonitoredDirectoryId == monitored.Id)
            .Select(x => x.MeanId)
            .ToHashSet();

        if (means.SetEquals(option.Means) == false)
        {
            foreach (var mean in option.Means.Where(x => !means.Contains(x)))
            {
                // ADD
                var io = new IndexingOption { MonitoredDirectoryId = monitored.Id, MeanId = mean };
                await context.IndexingOptions.AddAsync(io);
            }

            foreach (var mean in means.Where(x => !option.Means.Contains(x)))
            {
                // REMOVE
                var io = await context.IndexingOptions
                    .FirstOrDefaultAsync(x => x.MonitoredDirectoryId == monitored.Id && x.MeanId == mean);
                if (io is not null) context.IndexingOptions.Remove(io);
            }

            await context.SaveChangesAsync();
        }

        return changeRecursion;
    }


    private Task<Directory?> GetDirectoryByPath(string path)
    {
        return context.Directories.FirstOrDefaultAsync(x => x.Path == path);
    }

    private async Task<Directory> AddDirectory(string path)
    {
        var directory = new Directory { Path = path };
        await context.Directories.AddAsync(directory);
        await context.SaveChangesAsync();
        return directory;
    }

    private IEnumerable<Directory> GetDirectoryBranch(string path)
    {
        return context.Directories.Where(x => x.Path.StartsWith(path));
    }

    private Task<MonitoredDirectory?> GetOuterRecursivelyMonitoredDirectory(Directory directory)
    {
        return context.MonitoredDirectories
            .Include(x => x.Directory)
            .Where(x => x.Recursive && x.DirectoryId != directory.Id)
            .OrderBy(x => x.Directory.Path.Length)
            .FirstOrDefaultAsync(x => directory.Path.StartsWith(x.Directory.Path));
    }

    private async Task<List<MonitoredDirectory>> GetInnerMonitoredDirectories(Directory directory)
    {
        var directories = await context.MonitoredDirectories
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
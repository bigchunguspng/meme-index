using MemeIndex_Core.Data;
using MemeIndex_Core.Entities;
using MemeIndex_Core.Model;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Entities.Directory;

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

    /*
    update changes:
        - dir added to wl
                    +dir if ness, +md, +io, add all files
        - dir removed from wl
                    -md, -io, rem all files with md = md
        - dir recursive flag changed
                    to true ? add all files from subs : rem all files from subs, update fsw
        - dir mean list modified
                    for new options > trigger indexing (for this dir)
                    for removed > del tags where mean = x and file is in that dir
     */
    public async Task UpdateMonitoredDirectories(IList<MonitoredDirectory> directoriesModified)
    {
        throw new NotImplementedException();
        var directoriesDatabase = await _context.MonitoredDirectories
            .Include(x => x.Directory)
            .Include(x => x.IndexingOptions)
            .AsSplitQuery()
            .ToListAsync();

        var remList = directoriesDatabase.Except(directoriesModified).ToList();
        var addList = directoriesModified.Except(directoriesDatabase).ToList();
        var updList = directoriesDatabase.Union(directoriesModified).ToList();

        // rem >> 
    }

    public async Task<MonitoredDirectory> AddDirectory(MonitoringOptions options)
    {
        // todo handle recursion and nested directories
        // if this directory is inside of one that is monitored recursively >>
        // files should be tracked BY THE ONE WITH SHORTER PATH

        var directory = await GetDirectoryByPath(options.Path) ?? await AddDirectory(options.Path);

        var monitored = new MonitoredDirectory
        {
            DirectoryId = directory.Id,
            Recursive = options.Recursive
        };
        await _context.MonitoredDirectories.AddAsync(monitored);
        await _context.SaveChangesAsync();

        foreach (var mean in options.Means)
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

        /*var covering = await GetOuterRecursivelyMonitoredDirectory(directory);
        if (covering is not null)
        {
            AttachFilesToOtherMonitor(monitored, covering);

            await _context.SaveChangesAsync();

            _context.MonitoredDirectories.Remove(monitored);
        }
        else
        {
            var inner = await GetInnerMonitoredDirectories(directory);
            if (inner.Count > 0)
            {
                foreach (var replacement in inner) AttachFilesToOtherMonitor(monitored, replacement);
                await _context.SaveChangesAsync();
            }

            /*if (monitored.Recursive)
                _context.Directories.RemoveRange(GetDirectoryBranch(directory.Path)); // except these
            else#1#
            _context.Directories.Remove(directory); // this will cascade remove md and files with this directory as md
        }*/

        if (monitored.Recursive) _context.Directories.RemoveRange(GetDirectoryBranch(directory.Path));
        else /*               */ _context.Directories.Remove(directory);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateDirectory(MonitoringOptions options)
    {
        var directory = await GetDirectoryByPath(options.Path);
        if (directory is null) return;

        var monitored = await _context.MonitoredDirectories.FirstOrDefaultAsync(x => x.DirectoryId == directory.Id);
        if (monitored is null) return;

        if (monitored.Recursive != options.Recursive)
        {
            monitored.Recursive = options.Recursive;
            await _context.SaveChangesAsync();

            // files anyway will be added / removed by indexing service
            /*var covering = await GetOuterRecursivelyMonitoredDirectory(directory);
            if (covering is null)
            {
                if (options.Recursive)
                {
                    // trigger future file adding
                    // Fire event (pass monitoring directory)
                }
                else
                {
                    // del subdirs if they are
                    var subdirectories = GetDirectoryBranch(directory.Path).Where(x => x.Id != directory.Id);
                    _context.Directories.RemoveRange(subdirectories);
                    await _context.SaveChangesAsync();
                }
            }
            // else:
            // no files should be added (they already are / will)
            // no subdirs should be removed*/
        }

        var means = _context.IndexingOptions
            .Where(x => x.MonitoredDirectoryId == monitored.Id)
            .Select(x => x.MeanId)
            .ToHashSet();

        if (means.SetEquals(options.Means) == false)
        {
            foreach (var mean in options.Means.Where(x => !means.Contains(x)))
            {
                // ADD
                var option = new IndexingOption { MonitoredDirectoryId = monitored.Id, MeanId = mean };
                await _context.IndexingOptions.AddAsync(option);
            }

            foreach (var mean in means.Where(x => !options.Means.Contains(x)))
            {
                // REMOVE
                var option = await _context.IndexingOptions
                    .FirstOrDefaultAsync(x => x.MonitoredDirectoryId == monitored.Id && x.MeanId == mean);
                if (option is not null) _context.IndexingOptions.Remove(option);
            }

            await _context.SaveChangesAsync();
        }
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

    private void AttachFilesToOtherMonitor(MonitoredDirectory toRemove, MonitoredDirectory toReplace)
    {
        /*var files = _context.Files.Where(x => x.MonitoredDirectoryId == toRemove.Id);
        foreach (var file in files)
        {
            file.MonitoredDirectoryId = toReplace.Id;
        }*/
    }
}
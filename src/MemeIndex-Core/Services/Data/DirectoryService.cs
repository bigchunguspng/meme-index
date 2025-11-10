using MemeIndex_Core.Data;
using Directory = MemeIndex_Core.Data.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public class DirectoryService
{
    private readonly MemeDbContext _context;

    public DirectoryService(MemeDbContext context)
    {
        _context = context;
    }

    /// Returns all directories from the database.
    public IEnumerable<Directory> GetAll()
    {
        return _context.Directories;
    }

    /// Updates directory location.
    public async Task Update(string oldPath, string newPath)
    {
        foreach (var directory in GetDirectoryBranch(oldPath))
        {
            directory.Path = directory.Path.Replace(oldPath, newPath);
        }

        await _context.SaveChangesAsync();
    }

    /// Removes all directories that has no file records.
    /// <returns>Number of directories removed.</returns>
    public async Task<int> ClearEmpty()
    {
        var emptyDirectories = _context.Directories.Where
        (
            directory =>
                !_context.MonitoredDirectories.Any(x => x.DirectoryId == directory.Id) &&
                !_context.Files
                    .Select(file => file.DirectoryId)
                    .Distinct()
                    .Contains(directory.Id)
        );

        _context.Directories.RemoveRange(emptyDirectories);
        return await _context.SaveChangesAsync();
    }


    private IEnumerable<Directory> GetDirectoryBranch(string path)
    {
        return _context.Directories.Where(x => x.Path.StartsWith(path));
    }
}
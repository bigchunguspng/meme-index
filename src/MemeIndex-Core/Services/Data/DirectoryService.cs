using MemeIndex_Core.Data;
using MemeIndex_Core.Services.Data.Contracts;
using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public class DirectoryService : IDirectoryService
{
    private readonly MemeDbContext _context;

    public DirectoryService(MemeDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Directory> GetAll()
    {
        return _context.Directories;
    }

    public async Task Update(string oldPath, string newPath)
    {
        foreach (var directory in GetDirectoryBranch(oldPath))
        {
            directory.Path = directory.Path.Replace(oldPath, newPath);
        }

        await _context.SaveChangesAsync();
    }

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
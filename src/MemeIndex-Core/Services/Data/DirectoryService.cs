using MemeIndex_Core.Data;
using Microsoft.EntityFrameworkCore;
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

    public IEnumerable<Directory> GetTracked()
    {
        return _context.Directories.Where(x => x.IsTracked);
    }

    public async Task<Directory> AddTracking(string path)
    {
        var entity = await GetByPathAsync(path);
        if (entity == null)
        {
            entity = new Directory
            {
                Path = path,
                IsTracked = true
            };

            await _context.Directories.AddAsync(entity);
        }
        else
        {
            entity.IsTracked = true;
            _context.Directories.Update(entity);
        }

        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task RemoveTracking(string path)
    {
        var entity = await GetByPathAsync(path);
        if (entity is null)
        {
            return;
        }

        if (await IsInsideOtherTrackedDirectory(entity))
        {
            entity.IsTracked = false;
            _context.Directories.Update(entity);
        }
        else
        {
            _context.Directories.RemoveRange(GetDirectoryAndSubdirectories(entity.Path));
        }

        await _context.SaveChangesAsync();
    }

    public async Task Update(string oldPath, string newPath)
    {
        foreach (var directory in GetDirectoryAndSubdirectories(oldPath))
        {
            directory.Path = directory.Path.Replace(oldPath, newPath);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> ClearEmpty()
    {
        var emptyDirectories = _context.Directories.Where
        (
            dir => !dir.IsTracked && !_context.Files
                .Select(file => file.DirectoryId)
                .Distinct()
                .Contains(dir.Id)
        );

        _context.Directories.RemoveRange(emptyDirectories);
        return await _context.SaveChangesAsync();
    }


    private Task<Directory?> GetByPathAsync(string path)
    {
        return _context.Directories.FirstOrDefaultAsync(x => x.Path == path);
    }

    private IEnumerable<Directory> GetDirectoryAndSubdirectories(string path)
    {
        return _context.Directories.Where(x => x.Path.StartsWith(path));
    }

    private Task<bool> IsInsideOtherTrackedDirectory(Directory directory)
    {
        return _context.Directories
            .Where(x => x.IsTracked && x.Id != directory.Id)
            .AnyAsync(x => directory.Path.StartsWith(x.Path));
    }
}
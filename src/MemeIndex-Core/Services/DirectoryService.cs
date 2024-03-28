using MemeIndex_Core.Data;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services;

public class DirectoryService : IDirectoryService
{
    private readonly MemeDbContext _context;

    public DirectoryService(MemeDbContext context)
    {
        _context = context;
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
            _context.Directories.RemoveRange(GetDirectoryAndSubdirectories(entity));
        }

        await _context.SaveChangesAsync();
    }

    public async Task Update(string oldPath, string newPath)
    {
        var entity = await GetByPathAsync(oldPath);
        if (entity is null)
        {
            return;
        }

        foreach (var directory in GetDirectoryAndSubdirectories(entity))
        {
            directory.Path = directory.Path.Replace(oldPath, newPath);
            _context.Directories.Update(directory);
        }

        await _context.SaveChangesAsync();
    }


    private Task<Directory?> GetByPathAsync(string path)
    {
        return _context.Directories.FirstOrDefaultAsync(x => x.Path == path);
    }

    private IEnumerable<Directory> GetDirectoryAndSubdirectories(Directory directory)
    {
        return _context.Directories.Where(x => x.Path.StartsWith(directory.Path));
    }

    private Task<bool> IsInsideOtherTrackedDirectory(Directory directory)
    {
        return _context.Directories
            .Where(x => x.IsTracked && x.Id != directory.Id)
            .AnyAsync(x => directory.Path.StartsWith(x.Path));
    }
}
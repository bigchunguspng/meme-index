using MemeIndex_Core.Data;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public class FileService : IFileService
{
    private readonly MemeDbContext _context;

    public FileService(MemeDbContext context)
    {
        _context = context;
    }

    public async Task<IList<Entities.File>> GetAllFilesWithPath()
    {
        return await _context.Files.Include(x => x.Directory).ToListAsync();
    }

    public async Task<int> AddFile(FileInfo file)
    {
        var directory = await GetDirectoryEntity(file.DirectoryName!);

        var entity = new Entities.File
        {
            DirectoryId = directory.Id,
            Name = file.Name,
            Size = file.Length,
            Tracked = DateTime.UtcNow,
            Created = file.CreationTimeUtc,
            Modified = file.LastWriteTimeUtc
        };

        await _context.Files.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<int> UpdateFile(Entities.File entity, FileInfo file)
    {
        var directory = await GetDirectoryEntity(file.DirectoryName!);

        entity.DirectoryId = directory.Id;
        entity.Name = file.Name;
        entity.Size = file.Length;
        entity.Tracked = DateTime.UtcNow;
        entity.Created = file.CreationTimeUtc;
        entity.Modified = file.LastWriteTimeUtc;

        _context.Files.Update(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<int> RemoveRange(IEnumerable<Entities.File> files)
    {
        _context.Files.RemoveRange(files);
        return await _context.SaveChangesAsync();
    }

    private async Task<Directory> GetDirectoryEntity(string path)
    {
        var existing = _context.Directories.FirstOrDefault(x => x.Path == path);
        if (existing != null)
        {
            return existing;
        }

        var entity = new Directory { Path = path };

        await _context.Directories.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }
}
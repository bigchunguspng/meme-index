using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;
using Directory = MemeIndex_Core.Data.Entities.Directory;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Core.Services.Data;

public class FileService(MemeDbContext context)
{
    public async Task<IList<File>> GetAllFilesWithPath()
    {
        return await context.Files.Include(x => x.Directory).ToListAsync();
    }

    public Task<File?> TryGet(FileInfo file, string name) => context.Files.FirstOrDefaultAsync
    (
        x => x.Name == name
          && x.Size == file.Length
          && x.Modified == file.LastWriteTimeUtc
    );

    public async Task<int> AddFile(FileInfo file)
    {
        var directory = await GetOrAddDirectory(file.DirectoryName!);

        var entity = new File
        {
            DirectoryId = directory.Id,
            Name = file.Name,
            Size = file.Length,
            Tracked = DateTime.UtcNow,
            Created = file.CreationTimeUtc,
            Modified = file.LastWriteTimeUtc
        };

        await context.Files.AddAsync(entity);
        await context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<int> AddFiles(MonitoredDirectory monitoredDirectory)
    {
        var files = FileHelpers.GetImageFiles(monitoredDirectory.Directory.Path, monitoredDirectory.Recursive);

        var tasks = files.Select(async file =>
        {
            var directory = await GetOrAddDirectory(file.DirectoryName!);

            return new File
            {
                DirectoryId = directory.Id,
                Name = file.Name,
                Size = file.Length,
                Tracked = DateTime.UtcNow,
                Created = file.CreationTimeUtc,
                Modified = file.LastWriteTimeUtc
            };
        });

        var results = await Task.WhenAll(tasks);

        await context.Files.AddRangeAsync(results);
        await context.SaveChangesAsync();

        // todo check if file exists in db before adding

        return results.Length;
    }

    public async Task<int> UpdateFile(File entity, FileInfo file)
    {
        var directory = await GetOrAddDirectory(file.DirectoryName!);

        entity.DirectoryId = directory.Id;
        entity.Name = file.Name;
        entity.Size = file.Length;
        entity.Tracked = DateTime.UtcNow;
        entity.Created = file.CreationTimeUtc;
        entity.Modified = file.LastWriteTimeUtc;

        context.Files.Update(entity);
        await context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task RemoveRange(IEnumerable<File> files)
    {
        context.Files.RemoveRange(files);
        await context.SaveChangesAsync();
    }

    private async Task<Directory> GetOrAddDirectory(string path)
    {
        var existing = context.Directories.FirstOrDefault(x => x.Path == path);
        if (existing != null)
        {
            return existing;
        }

        var entity = new Directory { Path = path };

        await context.Directories.AddAsync(entity);
        await context.SaveChangesAsync();

        return entity;
    }
}
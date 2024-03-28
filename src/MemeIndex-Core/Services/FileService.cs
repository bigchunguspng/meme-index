using MemeIndex_Core.Data;
using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services;

public class FileService : IFileService
{
    private readonly MemeDbContext _context;

    public FileService(MemeDbContext context)
    {
        _context = context;
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
        var entry = await _context.Files.AddAsync(entity);

        await _context.SaveChangesAsync();

        return entry.Entity.Id;

        // ocr image, add text to db

        /*var text = await _ocrService.GetTextRepresentation(file.FullName, "eng");
        await _context.Texts.AddAsync(new Text
        {
            FileId = fileEntity.Id,
            MeanId = 2,
            Representation = text,
        });*/

        // color-tag image, add text to db (soon)
    }

    private async Task<Directory> GetDirectoryEntity(string path)
    {
        var entity = _context.Directories.FirstOrDefault(x => x.Path == path);
        if (entity != null)
        {
            return entity;
        }

        var entry = await _context.Directories.AddAsync(new Directory { Path = path });

        await _context.SaveChangesAsync();

        return entry.Entity;
    }
}
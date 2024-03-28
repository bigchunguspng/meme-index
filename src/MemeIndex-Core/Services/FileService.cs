using MemeIndex_Core.Data;

namespace MemeIndex_Core.Services;

public class FileService : IFileService
{
    private readonly MemeDbContext _context;
    //private readonly IOcrService _ocrService;

    public FileService(MemeDbContext context/*, IOcrService ocrService*/)
    {
        _context = context;
        //_ocrService = ocrService;
    }

    public async Task<int> IndexFile(FileInfo file)
    {
        // get file, add file to db
        var directory =
            _context.Directories.FirstOrDefault(x => x.Path == file.DirectoryName) ??
            (await _context.Directories.AddAsync(new Entities.Directory { Path = file.DirectoryName! })).Entity;

        return (await _context.Files.AddAsync(new Entities.File
        {
            DirectoryId = directory.Id,
            Name = file.Name,
            Size = file.Length,
            Tracked = DateTime.UtcNow,
            Created = file.CreationTimeUtc,
            Modified = file.LastWriteTimeUtc
        })).Entity.Id;

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
}
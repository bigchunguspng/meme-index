using MemeIndex_Core.Data;
using MemeIndex_Core.Entities;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.OCR;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;
using Directory = System.IO.Directory;

namespace MemeIndex_Core.Services.Indexing;

public class IndexingService
{
    private readonly FileWatchService _watch;
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;
    private readonly IMonitoringService _monitoringService;
    private readonly OvertakingService _overtakingService;
    private readonly MemeDbContext _context;
    private readonly IServiceProvider _services;
    private readonly OcrServiceResolver _ocrServiceResolver;

    public IndexingService
    (
        FileWatchService watch,
        IFileService fileService,
        IDirectoryService directoryService,
        IMonitoringService monitoringService,
        OvertakingService overtakingService,
        MemeDbContext context,
        IServiceProvider services,
        OcrServiceResolver ocrServiceResolver
    )
    {
        _watch = watch;
        _fileService = fileService;
        _directoryService = directoryService;
        _monitoringService = monitoringService;
        _overtakingService = overtakingService;
        _context = context;
        _services = services;
        _ocrServiceResolver = ocrServiceResolver;
    }

    public async Task ProcessPendingFiles()
    {
        var means = await _context.Means.ToListAsync();
        foreach (var mean in means)
        {
            await ProcessPendingFiles(mean);
        }
    }

    private async Task ProcessPendingFiles(Mean mean)
    {
        // get all db files w/o representation
        // ocr

        // mean -> io -> md -> directory -> files

        var monitored = _monitoringService.GetDirectories(mean.Id);
        var dirs1 = monitored
            .Where(x => x.Recursive == false)
            .Select(x => x.Directory.Id);
        var dirs2 = monitored
            .Where(x => x.Recursive == true)
            .SelectMany(x => _context.Directories.Where(d => d.Path.StartsWith(d.Path)))
            .Select(x => x.Id);
        var files = await _context.Files
            .Where(x => dirs1.Contains(x.DirectoryId) || dirs2.Contains(x.DirectoryId))
            .Where(x => !_context.Tags.Any(t => t.MeanId == mean.Id && t.FileId == x.Id))
            .ToListAsync();

        var ocrService = _ocrServiceResolver(mean.Id);
        foreach (var file in files)
        {
            var path = Path.Combine(file.Directory.Path, file.Name);
            var words = await ocrService.GetTextRepresentation(path);
            if (words is null)
            {
                words = new List<RankedWord> { new("[null]", 1) };
            }

            Logger.Log(ConsoleColor.Blue, $"OCR [{mean.Id}]");
            Logger.Log(ConsoleColor.Blue, string.Join(' ', words));

            var wordIds = new Queue<int>();
            foreach (var word in words)
            {
                var record = await GetOrAddWord(word.Word);
                wordIds.Enqueue(record.Id);
            }

            var tags = words.Select(x => new Tag
            {
                FileId = file.Id,
                WordId = wordIds.Dequeue(),
                MeanId = mean.Id,
                Rank = x.Rank,
            });
            await _context.Tags.AddRangeAsync(tags);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<Word> GetOrAddWord(string word)
    {
        var existing = _context.Words.FirstOrDefault(x => x.Text == word);
        if (existing != null)
        {
            return existing;
        }

        var entity = new Word { Text = word };

        await _context.Words.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    /*
    public Task<List<MonitoredDirectory>> GetTrackedDirectories()
    {
        return _monitoringService.GetDirectories();
    }
    */

    /*
    public async Task AddDirectory(MonitoringOptions options)
    {
        var path = options.Path;
        if (path.DirectoryExists())
        {
            Logger.Status($"Adding \"{path}\"...");

            // add to db, start watching
            await _monitoringService.AddDirectory(options);
            _watch.StartWatching(path, options.Recursive);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);

            // add to db all files
            var files = Helpers.GetImageFiles(path, options.Recursive);

            Logger.Log(ConsoleColor.Magenta, "Files: {0}", files.Count);

            var tasks = files.Select(file => _fileService.AddFile(file));
            await Task.WhenAll(tasks);

            Logger.Log("Done", ConsoleColor.Magenta);
        }
    }
    */

    /*
    /// <summary>
    /// This method should be called intentionally by user,
    /// because it removes all files from that directory from index.
    /// </summary>
    public async Task RemoveDirectory(string path)
    {
        // remove from db
        // stop watching

        if (path.DirectoryExists())
        {
            Logger.Status($"Removing \"{path}\"...");

            await _monitoringService.RemoveDirectory(path);
            _watch.StopWatching(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] removed", path);
        }
    }
    */

    public async Task<IEnumerable<MonitoredDirectory>> GetMissingDirectories()
    {
        var monitored = await _monitoringService.GetDirectories();
        return monitored.Where(x => !Directory.Exists(x.Directory.Path));
    }

    public void MoveDirectory(string oldPath, string newPath)
    {
        if (newPath.DirectoryExists())
        {
            _directoryService.Update(oldPath, newPath);
        }
    }

    private bool FileWasUpdated(FileInfo fileInfo, Entities.File entity)
    {
        // (so it needs reindexing by visual means)
        return fileInfo.Length != entity.Size ||
               fileInfo.LastWriteTimeUtc > entity.Tracked ||
               fileInfo.CreationTimeUtc  > entity.Tracked;
    }

    /*
    public async void StartIndexingAsync()
    {
        // todo ask to locate missing dirs

        await _overtakingService.OvertakeMissingFiles();

        // todo check all files for changes with FileWasUpdated()

        await _watch.Start();
    }
    */

    /*
    public void StopIndexing()
    {
        _watch.Stop();
    }
*/
}
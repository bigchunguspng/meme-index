using MemeIndex_Core.Data;
using MemeIndex_Core.Entities;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.ImageToText;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;
using Directory = System.IO.Directory;

namespace MemeIndex_Core.Services.Indexing;

public class IndexingService
{
    private readonly IDirectoryService _directoryService;
    private readonly IMonitoringService _monitoringService;
    private readonly MemeDbContext _context;
    private readonly ImageToTextServiceResolver _imageToTextServiceResolver;

    public IndexingService
    (
        IDirectoryService directoryService,
        IMonitoringService monitoringService,
        MemeDbContext context,
        ImageToTextServiceResolver imageToTextServiceResolver
    )
    {
        _directoryService = directoryService;
        _monitoringService = monitoringService;
        _context = context;
        _imageToTextServiceResolver = imageToTextServiceResolver;
    }

    public async Task ProcessPendingFiles()
    {
        Logger.Log(ConsoleColor.Magenta, "Processing files: start");

        var means = await _context.Means.ToListAsync();

        var processing = new ProcessingResults();

        await Task.WhenAll(means.Select(async mean => await ProcessPendingFiles(mean.Id, processing)));

        Logger.Log(ConsoleColor.Magenta, "Processing files: done!");
    }

    private class ProcessingResults : Dictionary<int, ProcessingResult>
    {
        private bool Done => Values.All(x => x.Done);

        public override string ToString()
        {
            var what = Done ? "done" : "files";
            return $"Processing {what}: {string.Join(' ', this.Select(x => $"Mean #{x.Key}: {x.Value}"))}";
        }
    }

    private class ProcessingResult
    {
        public int Processed, Total;

        public bool Done => Processed == Total;

        public override string ToString() => $"{Processed} / {Total}";
    }

    private async Task ProcessPendingFiles(int meanId, ProcessingResults processing)
    {
        var files = await GetPendingFiles(meanId);
        if (files.Count == 0) return;

        processing.Add(meanId, new ProcessingResult { Total = files.Count });
        Logger.Status(processing.ToString());

        var filesByPath = files.ToDictionary(x => x.GetFullPath(), x => x);
        var imageToTextService = _imageToTextServiceResolver(meanId);

        imageToTextService.ImageProcessed += async dictionary =>
        {
            var results = dictionary
                .Select(x => new ImageTextRepresentation(filesByPath[x.Key], x.Value, meanId))
                .ToList();
            await UpdateDatabase(results);

            processing[meanId].Processed += results.Count;
            Logger.Status(processing.ToString());
        };
        await imageToTextService.ProcessFiles(filesByPath.Keys);
    }

    /// <summary>
    /// Returns a list of <b>files</b>, located in directories,
    /// monitored by a <see cref="Mean"/> with the specified id,
    /// that have no related search tags.
    /// </summary>
    private Task<List<Entities.File>> GetPendingFiles(int meanId)
    {
        var monitored = _monitoringService.GetDirectories(meanId);
        var dirs1 = monitored
            .Where(x => x.Recursive == false)
            .Select(x => x.Directory.Id);
        var dirs2 = _context.Directories
            .Where(x => monitored.Any(m => m.Recursive && x.Path.StartsWith(m.Directory.Path)))
            .Select(x => x.Id);
        return _context.Files
            .Where(x => dirs1.Contains(x.DirectoryId) || dirs2.Contains(x.DirectoryId))
            .Where(x => !_context.Tags.Any(t => t.MeanId == meanId && t.FileId == x.Id))
            .ToListAsync();
    }

    private async Task UpdateDatabase(IEnumerable<ImageTextRepresentation> results)
    {
        await _context.Access.Take();

        foreach (var result in results)
        {
            var wordIds = new Queue<int>();
            foreach (var word in result.Words)
            {
                var record = await GetOrAddWord(word.Word);
                wordIds.Enqueue(record.Id);
            }

            var tags = result.Words.Select(x => new Tag
            {
                FileId = result.File.Id,
                WordId = wordIds.Dequeue(),
                MeanId = result.Mean,
                Rank = x.Rank,
            });
            await _context.Tags.AddRangeAsync(tags);
            await _context.SaveChangesAsync();
        }

        _context.Access.Release();
    }

    private async Task<Entities.Word> GetOrAddWord(string word)
    {
        var existing = _context.Words.FirstOrDefault(x => x.Text == word);
        if (existing != null)
        {
            return existing;
        }

        var entity = new Entities.Word { Text = word };

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

public record ImageTextRepresentation(Entities.File File, List<RankedWord> Words, int Mean);
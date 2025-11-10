using MemeIndex_Core.Data;
using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.ImageAnalysis;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;
using Directory = System.IO.Directory;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Core.Services.Indexing;

public class IndexingService
{
    private readonly DirectoryService _directoryService;
    private readonly MonitoringService _monitoringService;
    private readonly MemeDbContext _context;
    private readonly TagService _tagService;
    private readonly ImageToTextServiceResolver _imageToTextServiceResolver;

    public IndexingService
    (
        DirectoryService directoryService,
        MonitoringService monitoringService,
        MemeDbContext context,
        TagService tagService,
        ImageToTextServiceResolver imageToTextServiceResolver
    )
    {
        _directoryService = directoryService;
        _monitoringService = monitoringService;
        _context = context;
        _tagService = tagService;
        _imageToTextServiceResolver = imageToTextServiceResolver;
    }

    public async Task ProcessPendingFiles()
    {
        Logger.Log(ConsoleColor.Magenta, "Processing files: start");

        var means = await _context.Means.ToListAsync();

        var processing = new ProcessingResults();

        await Task.WhenAll(means.Select(async mean => await ProcessPendingFiles(mean, processing)));

        Logger.Log(ConsoleColor.Magenta, "Processing files: done!");
    }

    private class ProcessingResults : Dictionary<Mean, ProcessingResult>
    {
        private bool Done => Values.All(x => x.Done);

        public override string ToString()
        {
            var what = Done ? "done" : "files";
            return $"Processing {what}: {string.Join(' ', this.Select(x => $"{x.Key.Subtitle}: {x.Value}"))}";
        }
    }

    private class ProcessingResult
    {
        public int Processed, Total;

        public bool Done => Processed == Total;

        public override string ToString() => $"{Processed} / {Total}";
    }

    private async Task ProcessPendingFiles(Mean mean, ProcessingResults processing)
    {
        Logger.Log(ConsoleColor.Magenta, $"Mean #{mean.Id}: {nameof(ProcessPendingFiles)}");

        var files = await GetPendingFiles(mean.Id);
        if (files.Count == 0) return;

        processing.Add(mean, new ProcessingResult { Total = files.Count });
        Logger.Status(processing.ToString());

        var filesByPath = files.ToDictionary(x => x.GetFullPath(), x => x);
        var imageToTextService = _imageToTextServiceResolver(mean.Id);

        imageToTextService.ImageProcessed += async dictionary =>
        {
            var results = dictionary
                .Select(x => new ImageContent(filesByPath[x.Key], x.Value, mean.Id))
                .ToList();
            await UpdateDatabase(results);

            processing[mean].Processed += results.Count;
            Logger.Status(processing.ToString());
        };
        await imageToTextService.ProcessFiles(filesByPath.Keys);
    }

    /// Returns a list of <b>files</b>, located in directories,
    /// monitored by a <see cref="Mean"/> with the specified id,
    /// that have no related search tags.
    private Task<List<File>> GetPendingFiles(int meanId)
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

    private async Task UpdateDatabase(IEnumerable<ImageContent> results)
    {
        await _context.Access.WaitAsync();

        await _tagService.AddRange(results);

        _context.Access.Release();
    }


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
}

public record ImageContent(File File, List<RankedWord> Words, int MeanId);
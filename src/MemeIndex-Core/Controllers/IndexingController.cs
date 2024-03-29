using System.Diagnostics;
using MemeIndex_Core.Data;
using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Controllers;

public class IndexingController
{
    private readonly FileWatchService _watch;
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;
    private readonly MemeDbContext _context;

    public IndexingController
    (
        FileWatchService watch,
        IFileService fileService,
        IDirectoryService directoryService,
        MemeDbContext context
    )
    {
        _watch = watch;
        _fileService = fileService;
        _directoryService = directoryService;
        _context = context;
    }

    public async Task AddDirectory(string path)
    {
        if (path.IsDirectory())
        {
            // add to db, start watching
            await _directoryService.AddTracking(path);
            _watch.AddDirectory(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);

            // add to db all files
            var files = GetImageFiles(path);

            Logger.Log(ConsoleColor.Magenta, "Files: {0}", files.Count);

            var tasks = files.Select(file => _fileService.AddFile(file));
            await Task.WhenAll(tasks);

            Logger.Log("Done", ConsoleColor.Magenta);
        }
    }

    private static List<FileInfo> GetImageFiles(string path)
    {
        var directory = new DirectoryInfo(path);
        return Helpers.GetImageExtensions()
            .SelectMany(x => directory.GetFiles($"*{x}", SearchOption.AllDirectories))
            .ToList();
    }

    /// <summary>
    /// This method should be called intentionally by user,
    /// because it removes all files from that directory from index.
    /// </summary>
    public async Task RemoveDirectory(string path)
    {
        // remove from db
        // stop watching

        if (path.IsDirectory())
        {
            await _directoryService.RemoveTracking(path);
            _watch.RemoveDirectory(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] removed", path);
        }
    }

    public IEnumerable<Entities.Directory> GetMissingDirectories()
    {
        return _directoryService.GetTracked().Where(x => !Directory.Exists(x.Path));
    }

    public void MoveDirectory(string oldPath, string newPath)
    {
        if (newPath.IsDirectory())
        {
            _directoryService.Update(oldPath, newPath);
        }
    }

    public async Task OvertakeMissingFiles()
    {
        // get all files > check existence
        // filter missing > try find > update | remove

        // ON STARTUP
        // 1. a bunch of new files added to db
        //    [select files left join text]
        // 2. these files are slowly processed [by ocr ang color-tag] in the background

        var sw = new Stopwatch();
        sw.Start();
        Logger.Log("Overtaking: start", ConsoleColor.Yellow);

        var existingTrackedDirectories = _directoryService.GetTracked().Where(x => Directory.Exists(x.Path)).ToList();
        var existingDirectories = _context.Directories.AsEnumerable().Where(x => Directory.Exists(x.Path)).ToList();
        var directoriesByPath = existingDirectories.ToDictionary(x => x.Path);

        var files = existingTrackedDirectories.SelectMany(x => GetImageFiles(x.Path));
        var fileRecords = await _context.Files.Include(x => x.Directory).ToListAsync();

        Logger.Log("Overtaking: files here", ConsoleColor.Yellow);

        // files present in fs, but missing in db
        var unknownFiles = new List<FileInfo>();

        foreach (var fileInfo in files)
        {
            var unknownDirectory = directoriesByPath.TryGetValue(fileInfo.DirectoryName!, out var directory) == false;
            if (unknownDirectory)
            {
                unknownFiles.Add(fileInfo); // new directory (created / moved / renamed)
                continue;
            }

            var fileEntity = fileRecords.FirstOrDefault
            (
                file => file.DirectoryId == directory!.Id && file.Name == fileInfo.Name
            );
            if (fileEntity is null)
            {
                unknownFiles.Add(fileInfo); // new file (created / moved / renamed)
            }
        }

        Logger.Log("Overtaking: unknown files here", ConsoleColor.Yellow);

        // files present in db, but missing in fs
        var missingFiles = fileRecords
            .Where(x => existingDirectories.Select(dir => dir.Id).Contains(x.DirectoryId))
            .Where(x => !File.Exists(Path.Combine(x.Directory.Path, x.Name)))
            .ToList();

        Logger.Log("Overtaking: missing files here", ConsoleColor.Yellow);

        // try to match unknown files to missing ones

        var locatedMissingFiles = new List<Entities.File>();

        foreach (var unknownFile in unknownFiles)
        {
            var equivalent = missingFiles.FirstOrDefault(x => FilesAreEquivalent(unknownFile, x));
            if (equivalent != null)
            {
                await _fileService.UpdateFile(equivalent, unknownFile);
                locatedMissingFiles.Add(equivalent);
            }
            else
            {
                await _fileService.AddFile(unknownFile);
            }
        }

        Logger.Log("Overtaking: unknown files added / updated", ConsoleColor.Yellow);

        var lostFiles = missingFiles.Except(locatedMissingFiles);

        _context.Files.RemoveRange(lostFiles);
        await _context.SaveChangesAsync();

        Logger.Log("Overtaking: missing files removed", ConsoleColor.Yellow);

        var emptyDirectories = _context.Directories.Where
        (
            dir => !_context.Files
                .Select(file => file.DirectoryId)
                .Distinct()
                .Contains(dir.Id)
        );

        _context.Directories.RemoveRange(emptyDirectories);
        await _context.SaveChangesAsync();

        Logger.Log("Overtaking: empty directories removed", ConsoleColor.Yellow);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: elapsed {0}", sw.Elapsed);


        // WHEN NEW FILE(s) ADDED (spotted by file watcher)
        // 1. file change object added to purgatory
        // 2. after 0.5 second changes are interpreted and db is updated

        // files can be added~, changed~, removed^, renamed_, moved_
        // ~ process, ^ remove, _ update

        // IF FILE ADDED:
        // 3. file added to db
        // 4. file processed in the background
    }

    private static bool FilesAreEquivalent(FileInfo fileInfo, Entities.File entity)
    {
        var sum = 0;

        if (fileInfo.Name   == entity.Name) sum++;
        if (fileInfo.Length == entity.Size) sum++;
        if (fileInfo.LastWriteTimeUtc == entity.Modified) sum++;
        if (fileInfo. CreationTimeUtc == entity. Created) sum++;

        return sum > 1;
    }

    private bool FileWasUpdated(FileInfo fileInfo, Entities.File entity)
    {
        return fileInfo.Length != entity.Size ||
               fileInfo.LastWriteTimeUtc > entity.Tracked ||
               fileInfo.CreationTimeUtc  > entity.Tracked;
    }

    public void StartIndexing()
    {
        // todo ask to locate missing dirs

        OvertakeMissingFiles().Wait();

        _watch.Start();
    }

    public void StopIndexing()
    {
        _watch.Stop();
    }
}

public class SearchController
{
    // - search files
}
using System.Diagnostics;
using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Controllers;

public class IndexingController
{
    private readonly FileWatchService _watch;
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;

    public IndexingController
    (
        FileWatchService watch,
        IFileService fileService,
        IDirectoryService directoryService
    )
    {
        _watch = watch;
        _fileService = fileService;
        _directoryService = directoryService;
    }

    public IEnumerable<Entities.Directory> GetTrackedDirectories()
    {
        return _directoryService.GetTracked();
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
        // ON STARTUP
        // 1. a bunch of new files added to db
        //    [select files left join text]
        // 2. these files are slowly processed [by ocr ang color-tag] in the background

        var timer = new Stopwatch();
        timer.Start();
        Logger.Log("Overtaking: start", ConsoleColor.Yellow);

        var existingDirectoriesAll     = _directoryService.GetAll()    .GetExisting().ToList();
        var existingDirectoriesTracked = _directoryService.GetTracked().GetExisting().ToList();

        var files = existingDirectoriesTracked.SelectMany(x => GetImageFiles(x.Path)).ToList();
        var fileRecords = await _fileService.GetAllFilesWithPath();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files loaded", files.Count);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files records available", fileRecords.Count);

        var directoriesByPath = existingDirectoriesAll.ToDictionary(x => x.Path);

        var unknownFiles = files // that present in fs, but missing as records in db
            .Where(info =>
            {
                var unknown = directoriesByPath.TryGetValue(info.DirectoryName!, out var directory) == false;
                if (unknown) return true;

                return !fileRecords.Any(file => file.DirectoryId == directory!.Id && file.Name == info.Name);
            })
            .ToList();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} unknown files loaded", unknownFiles.Count);

        var missingFiles = fileRecords // that present in db, but missing as files in fs
            .Where(x =>
            {
                var directoryExist = existingDirectoriesAll.Select(dir => dir.Id).Contains(x.DirectoryId);
                if (directoryExist)
                {
                    var path = Path.Combine(x.Directory.Path, x.Name);
                    return !File.Exists(path);
                }

                return false;
            })
            .ToList();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files missing", missingFiles.Count);

        var locatedMissingFiles = new List<Entities.File>();
        var c0 = 0;

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
                c0++;
            }
        }

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} missing files located", locatedMissingFiles.Count);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} new files added", c0);

        var lostFiles = missingFiles.Except(locatedMissingFiles);

        var c1 = await _fileService.RemoveRange(lostFiles);

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} missing files removed", c1);

        var c2 = await _directoryService.ClearEmpty();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} empty directories removed", c2);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: elapsed {0}", timer.Elapsed);


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
        var similarity = 0;

        if (fileInfo.Name   == entity.Name) similarity++;
        if (fileInfo.Length == entity.Size) similarity++;
        if (fileInfo.LastWriteTimeUtc == entity.Modified) similarity++;
        if (fileInfo. CreationTimeUtc == entity. Created) similarity++;

        return similarity > 1;
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
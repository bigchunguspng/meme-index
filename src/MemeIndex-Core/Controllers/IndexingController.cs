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

    public void OvertakeMissingFiles()
    {
        // get all files > check existence
        // filter missing > try find > update | remove

        // ON STARTUP
        // 1. a bunch of new files added to db
        //    [select files left join text]
        // 2. these files are slowly processed [by ocr ang color-tag] in the background

        var directories = _directoryService.GetTracked().Where(x => Directory.Exists(x.Path)).ToList();
        var directoriesByPath = directories.ToDictionary(x => x.Path);

        // get existing files > filter missing in db

        var files = directories.SelectMany(x => GetImageFiles(x.Path));

        // files present in fs, but missing in db
        var unknownDirectoryFiles = new List<FileInfo>();
        var unknownFiles = new List<FileInfo>();

        foreach (var fileInfo in files)
        {
            var unknownDirectory = directoriesByPath.TryGetValue(fileInfo.DirectoryName!, out var directory) == false;
            if (unknownDirectory)
            {
                unknownDirectoryFiles.Add(fileInfo); // new directory (created / moved / renamed)
                continue;
            }

            var fileEntity = _context.Files.FirstOrDefault
            (
                file => file.DirectoryId == directory!.Id && file.Name == fileInfo.Name
            );
            if (fileEntity is null)
            {
                unknownFiles.Add(fileInfo); // new file (created / moved / renamed)
            }

            /*if (FileWasUpdated(fileInfo, fileEntity)) <-- these will be selected from db later, on processing phase
            {
                changed.Add(fileInfo); // file was edited (needs reprocessing)
            }*/
        }

        // files present in db, but missing in fs
        var missingFiles = _context.Files
            .Include(x => x.Directory)
            .Where(x => directories.Select(dir => dir.Id).Contains(x.DirectoryId))
            .Where(x => !File.Exists(Path.Combine(x.Directory.Path, x.Name)))
            .ToList();

        // try to match unknown files to missing ones
        //  unknownDirectoryFiles > check db on name ^ size ^ modified > 1 (renaming doesn't update modified date)
        //      Y: create new db-directory for these files, update files by id
        //      N: add to unknownFiles
        //  unknownFiles > check db on name ^ size ^ modified > 1
        //      Y: update files by id
        //      N: add to db as new

        // add unmatched

        // remove empty directories from db


        // WHEN NEW FILE(s) ADDED (spotted by file watcher)
        // 1. file change object added to purgatory
        // 2. after 0.5 second changes are interpreted and db is updated

        // files can be added~, changed~, removed^, renamed_, moved_
        // ~ process, ^ remove, _ update

        // IF FILE ADDED:
        // 3. file added to db
        // 4. file processed in the background
    }

    private bool FilesAreTheSame(FileInfo fileInfo, Entities.File entity)
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
        OvertakeMissingFiles();

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
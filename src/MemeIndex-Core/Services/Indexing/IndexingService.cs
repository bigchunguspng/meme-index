using System.Diagnostics;
using System.Text;
using MemeIndex_Core.Entities;
using MemeIndex_Core.Model;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Utils;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace MemeIndex_Core.Services.Indexing;

public class IndexingService
{
    private readonly FileWatchService _watch;
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;
    private readonly IMonitoringService _monitoringService;

    public IndexingService
    (
        FileWatchService watch,
        IFileService fileService,
        IDirectoryService directoryService,
        IMonitoringService monitoringService
    )
    {
        _watch = watch;
        _fileService = fileService;
        _directoryService = directoryService;
        _monitoringService = monitoringService;
    }

    public event Action<string?>? Log;

    public Task<List<MonitoredDirectory>> GetTrackedDirectories()
    {
        return _monitoringService.GetDirectories();
    }

    public async Task AddDirectory(DirectoryMonitoringOptions options)
    {
        var path = options.Path;
        if (path.IsDirectory())
        {
            Log?.Invoke($"Adding \"{path}\"...");

            // add to db, start watching
            await _monitoringService.AddDirectory(options);
            _watch.AddDirectory(path, options.Recursive);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);

            // add to db all files
            var files = GetImageFiles(path, options.Recursive);

            Logger.Log(ConsoleColor.Magenta, "Files: {0}", files.Count);

            var tasks = files.Select(file => _fileService.AddFile(file));
            await Task.WhenAll(tasks);

            Logger.Log("Done", ConsoleColor.Magenta);
        }
    }

    private static List<FileInfo> GetImageFiles(string path, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var directory = new DirectoryInfo(path);
        return Helpers.GetImageExtensions()
            .SelectMany(x => directory.GetFiles($"*{x}", searchOption))
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
            Log?.Invoke($"Removing \"{path}\"...");

            await _monitoringService.RemoveDirectory(path);
            _watch.RemoveDirectory(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] removed", path);
        }
    }

    public async Task<IEnumerable<MonitoredDirectory>> GetMissingDirectories()
    {
        var monitored = await _monitoringService.GetDirectories();
        return monitored.Where(x => !Directory.Exists(x.Directory.Path));
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
        Log?.Invoke("Overtaking file system changes...");

        var existingDirectoriesAll = _directoryService.GetAll().GetExisting().ToList();
        var existingDirectoriesTracked = await _monitoringService.GetDirectories();

        var files = existingDirectoriesTracked.SelectMany(x => GetImageFiles(x.Directory.Path, x.Recursive)).ToList();
        var fileRecords = await _fileService.GetAllFilesWithPath();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files loaded", files.Count);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files records available", fileRecords.Count);
        Log?.Invoke($"Overtaking {files.Count} files & {fileRecords.Count} records...");

        // directories that: are known to db + exist in fs
        var directoriesByPath = existingDirectoriesAll.ToDictionary(x => x.Path);

        var unknownFiles = files // that present in fs, but missing as records in db
            .Where(info =>
            {
                // file is unknown if its directory is unknown
                var directoryUnknown = !directoriesByPath.TryGetValue(info.DirectoryName!, out var directory);
                if (directoryUnknown) return true;

                // file is unknown if record for its name + path combination don't exist
                return !fileRecords.Any(file => file.DirectoryId == directory!.Id && file.Name == info.Name);
            })
            .ToList();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} unknown files loaded", unknownFiles.Count);

        var missingFiles = fileRecords // that present in db, but missing as files in fs
            .Where(x =>
            {
                var directoryMissing = !existingDirectoriesAll.Select(dir => dir.Id).Contains(x.DirectoryId);
                if (directoryMissing) return true;

                return !File.Exists(Path.Combine(x.Directory.Path, x.Name));
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
        if (Log != null)
        {
            var builder = new StringBuilder("Changes overtaken!");
            if (missingFiles.Count > 0)
                builder
                    .Append(" Missing files located: ")
                    .Append(locatedMissingFiles.Count).Append('/')
                    .Append( /* */ missingFiles.Count).Append('.');

            if (c0 > 0) builder.Append(" Files added: "  ).Append(c0).Append('.');
            if (c1 > 0) builder.Append(" Files removed: ").Append(c1).Append('.');

            Log(builder.ToString());
        }


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
        // (so it needs reindexing by visual means)
        return fileInfo.Length != entity.Size ||
               fileInfo.LastWriteTimeUtc > entity.Tracked ||
               fileInfo.CreationTimeUtc  > entity.Tracked;
    }

    public async void StartIndexingAsync()
    {
        // todo ask to locate missing dirs

        await OvertakeMissingFiles();

        // todo check all files for changes with FileWasUpdated()

        await _watch.Start();
    }

    public void StopIndexing()
    {
        _watch.Stop();
    }
}

public class Overtaker
{
    
}
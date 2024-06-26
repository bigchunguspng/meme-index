using System.Diagnostics;
using System.Text;
using MemeIndex_Core.Services.Data;
using MemeIndex_Core.Services.Data.Contracts;
using MemeIndex_Core.Utils;
using Directory = MemeIndex_Core.Data.Entities.Directory;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Core.Services.Indexing;

public class OvertakingService
{
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;
    private readonly IMonitoringService _monitoringService;
    private readonly TagService _tagService;

    public OvertakingService
    (
        IFileService fileService,
        IDirectoryService directoryService,
        IMonitoringService monitoringService,
        TagService tagService
    )
    {
        _fileService = fileService;
        _directoryService = directoryService;
        _monitoringService = monitoringService;
        _tagService = tagService;
    }

    /// <summary>
    /// Overtakes the changes in file system, and updates the database
    /// to represent the real state of file system.
    /// </summary>
    public async Task UpdateFileSystemKnowledge()
    {
        // ON STARTUP
        // 1. a bunch of new files added to db
        //    [select files left join text]
        // 2. these files are slowly processed [by ocr ang color-tag] in the background

        // LOAD

        var timer = new Stopwatch();
        timer.Start();
        Logger.Log(ConsoleColor.Yellow, "Overtaking: start");
        Logger.Status("Updating database...");

        var existingDirectoriesAll = _directoryService.GetAll().GetExisting().ToList();
        var existingDirectoriesTracked = await _monitoringService.GetDirectories();

        var files = existingDirectoriesTracked.SelectMany(x => FileHelpers.GetImageFiles(x.Directory.Path, x.Recursive)).ToList();
        var fileRecords = await _fileService.GetAllFilesWithPath();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files loaded", files.Count);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files records available", fileRecords.Count);
        Logger.Status($"Comparing {files.Count} files & {fileRecords.Count} records...");

        // ANALYZE

        var unknownFiles = GetUnknownFiles(fileRecords, files, existingDirectoriesAll); // unknown to db

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} unknown files", unknownFiles.Count);

        var missingFiles = GetMissingFiles(fileRecords, existingDirectoriesAll); // missing in file system

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} files missing", missingFiles.Count);

        // LOCATING / ADDING / REMOVING

        var locatedMissingFiles = new List<File>();
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

        var lostFiles = missingFiles.Except(locatedMissingFiles).ToArray();

        await _fileService.RemoveRange(lostFiles);
        var c1 = lostFiles.Length;

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} missing files removed", c1);

        var c2 = await _directoryService.ClearEmpty();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} empty directories removed", c2);
        
        var c3 = await UnindexUpdatedFiles();

        Logger.Log(ConsoleColor.Yellow, "Overtaking: {0} updated files unindexed", c3);
        Logger.Log(ConsoleColor.Yellow, "Overtaking: elapsed {0}", timer.Elapsed);

        if (Logger.StatusIsAvailable)
        {
            var builder = new StringBuilder("Database updated!");
            if (missingFiles.Count > 0)
                builder
                    .Append(" Missing files located: ")
                    .Append(locatedMissingFiles.Count).Append('/')
                    .Append( /* */ missingFiles.Count).Append('.');

            if (c0 > 0) builder.Append(" Files added: "  ).Append(c0).Append('.');
            if (c1 > 0) builder.Append(" Files removed: ").Append(c1).Append('.');

            Logger.Status(builder.ToString());
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

    /// <summary>
    /// Removes search tags for each file that was updated.
    /// </summary>
    /// <returns> The number of updated files. </returns>
    private async Task<int> UnindexUpdatedFiles()
    {
        var fileRecords = await _fileService.GetAllFilesWithPath();
        var updated = fileRecords
            .Select(x => new
            {
                File = x,
                FileInfo = new FileInfo(x.GetFullPath())
            })
            .Where(x => FileWasUpdated(x.FileInfo, x.File))
            .ToList();

        foreach (var file in updated)
        {
            await _fileService.UpdateFile(file.File, file.FileInfo);
        }

        var fileIds = updated.Select(x => x.File.Id);
        await _tagService.RemoveTagsFromFiles(fileIds);

        return updated.Count;
    }

    /// <summary>
    /// Returns a list of files that are present
    /// as files in File System, but missing as records in Database.
    /// </summary>
    private static List<FileInfo> GetUnknownFiles
    (
        IList<File> fileRecords,
        IEnumerable<FileInfo> files,
        IEnumerable<Directory> existingDirectories
    )
    {
        // directories that: are known to db + exist in fs
        var directoriesByPath = existingDirectories.ToDictionary(x => x.Path);

        return files // that present in fs, but missing as records in db
            .Where(info =>
            {
                // file is unknown if its directory is unknown
                var directoryUnknown = !directoriesByPath.TryGetValue(info.DirectoryName!, out var directory);
                if (directoryUnknown) return true;

                // file is unknown if record for its name + path combination don't exist
                return !fileRecords.Any(file => file.DirectoryId == directory!.Id && file.Name == info.Name);
            })
            .ToList();
    }

    /// <summary>
    /// Returns a list of files that are present
    /// as records in Database, but missing as files in File System.
    /// </summary>
    private static List<File> GetMissingFiles
    (
        IList<File> fileRecords,
        IEnumerable<Directory> existingDirectories
    )
    {
        return fileRecords
            .Where(x =>
            {
                var directoryMissing = !existingDirectories.Select(dir => dir.Id).Contains(x.DirectoryId);
                if (directoryMissing) return true;

                return !x.GetFullPath().FileExists();
            })
            .ToList();
    }

    /// <summary>
    /// Returns true if both objects refers to a file with the same content.
    /// </summary>
    private static bool FilesAreEquivalent(FileInfo fileInfo, File entity)
    {
        var similarity = 0;

        if (fileInfo.Name   == entity.Name) similarity++;
        if (fileInfo.Length == entity.Size) similarity++;
        if (fileInfo.LastWriteTimeUtc == entity.Modified) similarity++;
        if (fileInfo. CreationTimeUtc == entity. Created) similarity++;

        return similarity > 1;
    }

    /// <summary>
    /// Returns true if the file content was probably changed.
    /// </summary>
    private static bool FileWasUpdated(FileInfo fileInfo, File entity)
    {
        return fileInfo.Length != entity.Size
            || fileInfo.LastWriteTimeUtc > entity.Tracked
            || fileInfo.CreationTimeUtc  > entity.Tracked;
    }
}
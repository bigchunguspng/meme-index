using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Controllers;

public class IndexingController
{
    private readonly FileWatchService _watch;
    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;

    public IndexingController(FileWatchService watch, IFileService fileService, IDirectoryService directoryService)
    {
        _watch = watch;
        _fileService = fileService;
        _directoryService = directoryService;
    }

    public async Task AddDirectory(string path)
    {
        if (path.IsDirectory())
        {
            // add to db, start watching
            await _directoryService.Add(path);
            _watch.AddDirectory(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);

            // add to db all files
            var directory = new DirectoryInfo(path);
            var files = Helpers.GetImageExtensions()
                .Select(x => directory.GetFiles($"*{x}", SearchOption.AllDirectories))
                .SelectMany(x => x)
                .ToList();

            Logger.Log(ConsoleColor.Magenta, "Files: {0}", files.Count);

            var tasks = files.Select(file => _fileService.AddFile(file));
            await Task.WhenAll(tasks);

            Logger.Log("Done", ConsoleColor.Magenta);
        }
    }

    public async Task RemoveDirectory(string path)
    {
        // remove from db
        // stop watching

        if (path.IsDirectory())
        {
            await _directoryService.Remove(path);
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

    public void GetMissingFiles()
    {
        // get all files > check existence
        // filter missing > try find > update | remove
    }

    public void StartIndexing()
    {
        // todo overtake file system changes

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
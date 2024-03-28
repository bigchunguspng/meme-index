using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Controllers;

public class IndexingController
{
    private readonly FileWatchService _watch;
    private readonly IDirectoryService _directoryService;

    public IndexingController(FileWatchService watch, IDirectoryService directoryService)
    {
        _watch = watch;
        _directoryService = directoryService;
    }

    public async Task AddDirectory(string path)
    {
        // add to db
        // start watching

        if (path.IsDirectory())
        {
            await _directoryService.Add(path);
            _watch.AddDirectory(path);

            Logger.Log(ConsoleColor.Magenta, "Directory [{0}] added", path);
            
            // add all files
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

    public IEnumerable<Entities.Directory> GetNotFoundDirectories()
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

    public void StartIndexing()
    {
        // todo overtake file system changes
        // start tracking

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
using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services;

public interface IDirectoryService
{
    /// <summary>
    /// Returns all directories from the database.
    /// </summary>
    IEnumerable<Directory> GetAll();

    /// <summary>
    /// Returns directories from tracking list.
    /// </summary>
    IEnumerable<Directory> GetTracked();

    /// <summary>
    /// Adds directory to tracking list.
    /// </summary>
    Task<Directory> AddTracking(string path);

    /// <summary>
    /// Removes directory from the tracking list.
    /// </summary>
    Task RemoveTracking(string path);

    /// <summary>
    /// Updates directory location.
    /// </summary>
    Task Update(string oldPath, string newPath);

    /// <summary>
    /// Removes all directories that has no file records.
    /// </summary>
    /// <returns>Number of directories removed.</returns>
    Task<int> ClearEmpty();
}
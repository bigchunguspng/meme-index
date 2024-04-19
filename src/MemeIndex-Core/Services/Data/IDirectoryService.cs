using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public interface IDirectoryService
{
    /// <summary>
    /// Returns all directories from the database.
    /// </summary>
    IEnumerable<Directory> GetAll();

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
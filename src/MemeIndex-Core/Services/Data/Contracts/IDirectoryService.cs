using Directory = MemeIndex_Core.Data.Entities.Directory;

namespace MemeIndex_Core.Services.Data.Contracts;

public interface IDirectoryService
{
    /// Returns all directories from the database.
    IEnumerable<Directory> GetAll();

    /// Updates directory location.
    Task Update(string oldPath, string newPath);

    /// Removes all directories that has no file records.
    /// <returns>Number of directories removed.</returns>
    Task<int> ClearEmpty();
}
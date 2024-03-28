using Directory = MemeIndex_Core.Entities.Directory;

namespace MemeIndex_Core.Services;

public interface IDirectoryService
{
    IEnumerable<Directory> GetTracked();

    Task<Directory> Add(string path);

    Task Update(string oldPath, string newPath);
    Task Remove(string path);
}
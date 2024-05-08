using MemeIndex_Core.Entities;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Core.Services.Data.Contracts;

public interface IFileService
{
    Task<IList<File>> GetAllFilesWithPath();

    Task<int> AddFile(FileInfo file);

    Task<int> AddFiles(MonitoredDirectory monitoredDirectory);

    Task<int> UpdateFile(File entity, FileInfo file);

    /// <returns>Number of files removed.</returns>
    Task<int> RemoveRange(IEnumerable<File> files);
}
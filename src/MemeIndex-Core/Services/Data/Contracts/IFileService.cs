using MemeIndex_Core.Data.Entities;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Core.Services.Data.Contracts;

public interface IFileService
{
    Task<IList<File>> GetAllFilesWithPath();

    Task<File?> TryGet(FileInfo file, string name);

    Task<int> AddFile(FileInfo file);

    Task<int> AddFiles(MonitoredDirectory monitoredDirectory);

    Task<int> UpdateFile(File entity, FileInfo file);

    Task RemoveRange(IEnumerable<File> files);
}
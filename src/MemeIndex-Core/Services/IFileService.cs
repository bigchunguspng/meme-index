namespace MemeIndex_Core.Services;

public interface IFileService
{
    Task<IList<Entities.File>> GetAllFilesWithPath();

    Task<int> AddFile(FileInfo file);

    Task<int> UpdateFile(Entities.File entity, FileInfo file);

    /// <returns>Number of files removed.</returns>
    Task<int> RemoveRange(IEnumerable<Entities.File> files);

    //Task UpdateFile(int id);
    //Task RemoveFile(int id);
}
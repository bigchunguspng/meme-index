namespace MemeIndex_Core.Services;

public interface IFileService
{
    Task<int> IndexFile(FileInfo file);

    //Task UpdateFile(int id);
    //Task RemoveFile(int id);
}
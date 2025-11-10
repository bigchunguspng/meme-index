using MemeIndex_Core.Data;
using Directory = MemeIndex_Core.Data.Entities.Directory;

namespace MemeIndex_Core.Services.Data;

public class DirectoryService(MemeDbContext context)
{
    /// Returns all directories from the database.
    public IEnumerable<Directory> GetAll()
    {
        return context.Directories;
    }

    /// Updates directory location.
    public async Task Update(string oldPath, string newPath)
    {
        foreach (var directory in GetDirectoryBranch(oldPath))
        {
            directory.Path = directory.Path.Replace(oldPath, newPath);
        }

        await context.SaveChangesAsync();
    }

    /// Removes all directories that has no file records.
    /// <returns>Number of directories removed.</returns>
    public async Task<int> ClearEmpty()
    {
        var emptyDirectories = context.Directories.Where
        (
            directory =>
                !context.MonitoredDirectories.Any(x => x.DirectoryId == directory.Id) &&
                !context.Files
                    .Select(file => file.DirectoryId)
                    .Distinct()
                    .Contains(directory.Id)
        );

        context.Directories.RemoveRange(emptyDirectories);
        return await context.SaveChangesAsync();
    }


    private IEnumerable<Directory> GetDirectoryBranch(string path)
    {
        return context.Directories.Where(x => x.Path.StartsWith(path));
    }
}
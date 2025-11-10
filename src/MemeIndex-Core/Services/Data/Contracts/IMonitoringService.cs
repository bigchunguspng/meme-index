using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Objects;

namespace MemeIndex_Core.Services.Data.Contracts;

public interface IMonitoringService
{
    /// Returns a list of monitored directories, including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties.
    Task<List<MonitoredDirectory>> GetDirectories();

    /// Returns a list of monitored directories, [including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties],
    /// which should be indexed by a <see cref="Mean"/> with given id.
    IQueryable<MonitoredDirectory> GetDirectories(int meanId);

    /// Adds a directory to monitoring list according to provided options.
    Task<MonitoredDirectory> AddDirectory(MonitoringOption option);

    /// Removes directory from monitoring list.
    /// It also removes all of its files if the directory isn't located
    /// inside of another directory, that is <b>recursively</b> monitored.
    Task RemoveDirectory(string path);

    /// Updates monitoring options of the directory.
    /// <returns> The value indicating whether the recursion option was altered. </returns>
    Task<bool> UpdateDirectory(MonitoringOption option);
}
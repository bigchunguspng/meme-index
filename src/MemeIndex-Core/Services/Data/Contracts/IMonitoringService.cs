using MemeIndex_Core.Data.Entities;
using MemeIndex_Core.Objects;

namespace MemeIndex_Core.Services.Data.Contracts;

public interface IMonitoringService
{
    /// <summary>
    /// Returns a list of monitored directories, including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties.
    /// </summary>
    Task<List<MonitoredDirectory>> GetDirectories();

    /// <summary>
    /// Returns a list of monitored directories, [including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties],
    /// which should be indexed by a <see cref="Mean"/> with given id.
    /// </summary>
    IQueryable<MonitoredDirectory> GetDirectories(int meanId);

    /// <summary>
    /// Adds a directory to monitoring list according to provided options.
    /// </summary>
    Task<MonitoredDirectory> AddDirectory(MonitoringOption option);

    /// <summary>
    /// Removes directory from monitoring list.
    /// It also removes all of its files if the directory isn't located
    /// inside of another directory, that is <b>recursively</b> monitored.
    /// </summary>
    Task RemoveDirectory(string path);

    /// <summary>
    /// Updates monitoring options of the directory.
    /// </summary>
    /// <returns> The value indicating whether the recursion option was altered. </returns>
    Task<bool> UpdateDirectory(MonitoringOption option);
}
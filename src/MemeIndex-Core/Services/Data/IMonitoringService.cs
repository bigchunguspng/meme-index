using MemeIndex_Core.Entities;
using MemeIndex_Core.Model;

namespace MemeIndex_Core.Services.Data;

public interface IMonitoringService
{
    /// <summary>
    /// Returns a list of monitored directories, including
    /// <see cref="MonitoredDirectory.Directory"/> and
    /// <see cref="MonitoredDirectory.IndexingOptions"/> properties.
    /// </summary>
    Task<List<MonitoredDirectory>> GetDirectories();

    // user updates watching list
    /// <summary>
    /// Updates 
    /// </summary>
    /// <param name="directories"></param>
    Task UpdateMonitoredDirectories(IList<MonitoredDirectory> directories);

    /*

    update changes:
        - dir added to wl
                    +dir if ness, +md, +io, add all files
        - dir removed from wl
                    -md, -io, rem all files with md = md
        - dir recursive flag changed
                    to true ? add all files from subs : rem all files from subs, update fsw
        - dir mean list modified
                    for new options > trigger indexing (for this dir)
                    for removed > del tags where mean = x and file is in that dir

     */

    /// <summary>
    /// Adds a directory to monitoring list according to provided options.
    /// </summary>
    Task AddDirectory(MonitoringOptions options);
    
    /// <summary>
    /// Removes directory from monitoring list.
    /// It also removes all of its files if the directory isn't located
    /// inside of another directory, that is <b>recursively</b> monitored.
    /// </summary>
    Task RemoveDirectory(string path);

    /// <summary>
    /// Updates monitoring options of the directory.
    /// </summary>
    Task UpdateDirectory(MonitoringOptions options);
}
using System.ComponentModel.DataAnnotations.Schema;

namespace MemeIndex_Core.Entities;

/// <summary>
/// <b>Directory</b> that is indexed and monitored for file changes.
/// </summary>
[Table("MonitoredDirectories")]
public class MonitoredDirectory : AbstractEntity
{
    public int DirectoryId { get; set; }

    /// <summary>
    /// Whether to track files from child folders.
    /// </summary>
    public bool Recursive { get; set; }

    public Directory Directory { get; set; } = default!;
    public ICollection<IndexingOption> IndexingOptions { get; set; } = default!;
}
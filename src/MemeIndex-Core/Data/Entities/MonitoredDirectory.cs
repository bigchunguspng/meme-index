using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data.Entities;

/// <b>Directory</b> that is indexed and monitored for file changes.
[Table("MonitoredDirectories"), Index(nameof(DirectoryId), IsUnique = true)]
public class MonitoredDirectory : AbstractEntity
{
    public int DirectoryId { get; set; }

    /// Whether to track files from child folders.
    public bool Recursive { get; set; }

    public Directory Directory { get; set; } = default!;
    public ICollection<IndexingOption> IndexingOptions { get; set; } = default!;
}
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Entities;

/// <summary>
/// An <b>image file</b>.
/// </summary>
[Index(nameof(DirectoryId), nameof(Name), IsUnique = true)]
public class File : AbstractEntity
{
    public int          DirectoryId { get; set; }
    public int MonitoredDirectoryId { get; set; }

    [Required] public string Name { get; set; } = default!;

    public long Size { get; set; }

    [Required] public DateTime Tracked  { get; set; }
    [Required] public DateTime Created  { get; set; }
    [Required] public DateTime Modified { get; set; }

    /// <summary>
    /// Directory that defines indexing options for this file.
    /// It can be different from the <see cref="Directory"/> property
    /// if the directory is monitored recursively.
    /// </summary>
    public MonitoredDirectory MonitoredDirectory { get; set; } = default!;

    /// <summary>
    /// Direct parent directory of this file.
    /// </summary>
    public Directory   Directory { get; set; } = default!;
    public ICollection<Tag> Tags { get; set; } = default!;
}
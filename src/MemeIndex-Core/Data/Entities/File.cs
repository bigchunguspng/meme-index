using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data.Entities;

/// An <b>image file</b>.
[Index(nameof(DirectoryId), nameof(Name), IsUnique = true)]
public class File : AbstractEntity
{
    public int          DirectoryId { get; set; }
    //public int MonitoredDirectoryId { get; set; }

    [Required] public string Name { get; set; } = default!;

    public long Size { get; set; }

    [Required] public DateTime Tracked  { get; set; }
    [Required] public DateTime Created  { get; set; }
    [Required] public DateTime Modified { get; set; }

    /*
    /// Directory that defines indexing options for this file.
    /// It can be different from the <see cref="Directory"/> property
    /// if the directory is monitored recursively.
    public MonitoredDirectory MonitoredDirectory { get; set; } = default!;
    */

    /// Direct parent directory of this file.
    public Directory   Directory { get; set; } = default!;
    public ICollection<Tag> Tags { get; set; } = default!;

    /// Make sure <see cref="Directory"/> is not null when calling this!
    public string GetFullPath() => Path.Combine(Directory.Path, Name);
}
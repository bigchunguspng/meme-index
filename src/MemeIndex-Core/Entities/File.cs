using System.ComponentModel.DataAnnotations;

namespace MemeIndex_Core.Entities;

/// <summary>
/// Represents an image file.
/// </summary>
public class File : AbstractEntity
{
    public int DirectoryId { get; set; }

    [Required] public string Name { get; set; } = default!;

    public long Size { get; set; }

    [Required] public DateTime Tracked  { get; set; }
    [Required] public DateTime Created  { get; set; }
    [Required] public DateTime Modified { get; set; }

    // created + size + path is used to detect changes to files on app startup
    // files where modified > tracked will be processed and updated
    
    public Directory     Directory { get; set; } = default!;
    public ICollection<Text> Texts { get; set; } = default!;
}
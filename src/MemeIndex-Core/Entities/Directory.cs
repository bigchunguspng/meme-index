using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemeIndex_Core.Entities;

/// <summary>
/// Since user tracks only specific folders, it is better to store them here
/// </summary>
[Table("Directories")]
public class Directory : AbstractEntity
{
    [Required] public string Path { get; set; } = default!;

    /// <summary>
    /// False for subdirectories
    /// </summary>
    public bool IsTracked { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace MemeIndex_Core.Entities;

/// <summary>
/// Since user tracks only specific folders, it is be better to move them here
/// </summary>
public class Directory : AbstractEntity
{
    [Required] public string Path { get; set; } = default!;
}
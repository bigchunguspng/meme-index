using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Data.Entities;

/// Since user tracks only specific folders, it is better to store them here
[Table("Directories"), Index(nameof(Path), IsUnique = true)]
public class Directory : AbstractEntity
{
    [Required] public string Path { get; set; } = default!;

    public ICollection<File> Files { get; set; } = default!;

    //public Directory? Parent { get; set; } // (just an idea)
}
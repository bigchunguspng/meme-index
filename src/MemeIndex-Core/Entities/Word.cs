using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Entities;

/// <summary>
/// Represents a word (text w/o spaces) that can be used in the representation.
/// </summary>
[Index(nameof(Text), IsUnique = true)]
public class Word : AbstractEntity
{
    [Required, RegularExpression(@"\S+")]
    public string Text { get; set; } = default!;
}
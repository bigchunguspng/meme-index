using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Entities;

/// <summary>
/// A <b>word</b> (text w/o spaces) that can be used as image search tag.
/// </summary>
[Index(nameof(Text), IsUnique = true)]
public class Word : AbstractEntity
{
    [Required, RegularExpression(@"\S+")]
    public string Text { get; set; } = default!;

    public ICollection<Tag> Tags { get; set; } = default!;
}
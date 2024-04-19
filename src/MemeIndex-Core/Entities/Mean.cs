using System.ComponentModel.DataAnnotations;

namespace MemeIndex_Core.Entities;

/// <summary>
/// A <b>method</b> for representing image contents.
/// </summary>
public class Mean : AbstractEntity
{
    /// <summary>
    /// Language code or "color"
    /// </summary>
    [Required] public string Code { get; set; } = default!;
}
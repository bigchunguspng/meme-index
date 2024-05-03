using System.ComponentModel.DataAnnotations;

namespace MemeIndex_Core.Entities;

/// <summary>
/// A <b>method</b> for representing image content.
/// </summary>
public class Mean : AbstractEntity
{
    public const int RGB_CODE = 1, ENG_CODE = 2;

    /// <summary>
    /// Language code (short, technical).
    /// </summary>
    [Required] public string Code { get; set; } = default!;

    /// <summary>
    /// To be used in UI (short, like single word).
    /// </summary>
    [Required] public string Title { get; set; } = default!;

    /// <summary>
    /// To be used in UI (longer, more descriptive).
    /// </summary>
    [Required] public string Subtitle { get; set; } = default!;

}
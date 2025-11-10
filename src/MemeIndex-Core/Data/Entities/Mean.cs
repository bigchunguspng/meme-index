using System.ComponentModel.DataAnnotations;

namespace MemeIndex_Core.Data.Entities;

/// A <b>method</b> for representing image content.
public class Mean : AbstractEntity
{
    public const int RGB_CODE = 1, ENG_CODE = 2;

    /// Language code (short, technical).
    [Required] public string Code { get; set; } = default!;

    /// To be used in UI (short, like single word).
    [Required] public string Title { get; set; } = default!;

    /// To be used in UI (longer, more descriptive).
    [Required] public string Subtitle { get; set; } = default!;

}
namespace MemeIndex_Core.Entities;

/// <summary>
/// Represents a word from the representation of an image file, that can be used to find it.
/// </summary>
public class Text : AbstractEntity
{
    public int FileId { get; set; }
    public int WordId { get; set; }
    public int MeanId { get; set; }

    /// <summary>
    /// Rank of the word in the description.
    /// The <b>less</b> is <b>better</b>.
    /// </summary>
    public int Rank { get; set; }

    public File File { get; set; } = default!;
    public Word Word { get; set; } = default!;
    public Mean Mean { get; set; } = default!;
}
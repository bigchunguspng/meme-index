namespace MemeIndex_Core.Data.Entities;

/// <summary>
/// <b>Search tag</b> that can be used to find an image file.
/// </summary>
public class Tag : AbstractEntity
{
    public const int MAX_RANK = 10_000;

    public int FileId { get; set; }
    public int WordId { get; set; }
    public int MeanId { get; set; }

    /// <summary>
    /// Rank of the word in the description.
    /// Range: [0 - 10K]
    /// <br/>
    /// The <b>MORE</b> is <b>better</b>.
    /// </summary>
    public int Rank { get; set; }

    public File File { get; set; } = default!;
    public Word Word { get; set; } = default!;
    public Mean Mean { get; set; } = default!;
}
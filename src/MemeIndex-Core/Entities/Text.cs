namespace MemeIndex_Core.Entities;

/// <summary>
/// Represents text representation of an image file, that can be used to find it.
/// </summary>
public class Text : AbstractEntity
{
    public int FileId { get; set; }
    public int MeanId { get; set; }

    public string? Representation { get; set; }

    public File File { get; set; } = default!;
    public Mean Mean { get; set; } = default!;
}
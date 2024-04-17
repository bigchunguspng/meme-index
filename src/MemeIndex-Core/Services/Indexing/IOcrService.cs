namespace MemeIndex_Core.Services.Indexing;

public interface IOcrService
{
    /// <summary>
    /// Returns a text representation of an image.
    /// </summary>
    /// <param name="path">Filepath to the image.</param>
    /// <param name="lang">Language code.</param>
    Task<IList<RankedWord>?> GetTextRepresentation(string path, string lang);
}

public record RankedWord(string Word, int Rank)
{
    public override string ToString() => $"#{Rank}\t{Word}";
}
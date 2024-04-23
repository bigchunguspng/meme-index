namespace MemeIndex_Core.Services.OCR;

public interface IOcrService
{
    /// <summary>
    /// Returns a text representation of an image.
    /// </summary>
    /// <param name="path">Filepath to the image.</param>
    Task<IList<RankedWord>?> GetTextRepresentation(string path);
}

public delegate IOcrService OcrServiceResolver(int key);

public record RankedWord(string Word, int Rank)
{
    public override string ToString() => $"#{Rank}\t{Word}";
}
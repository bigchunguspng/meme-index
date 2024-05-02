namespace MemeIndex_Core.Services.OCR;

public interface IOcrService
{
    /// <summary>
    /// Returns a text representation for each image file.
    /// </summary>
    /// <param name="paths"> Paths to the image files. </param>
    Task<Dictionary<string, IList<RankedWord>?>> ProcessFiles(IEnumerable<string> paths);

    /// <summary>
    /// Returns a text representation of an image.
    /// </summary>
    /// <param name="path"> Path to the image. </param>
    Task<IList<RankedWord>?> GetTextRepresentation(string path);
}

public delegate IOcrService OcrServiceResolver(int key);

public record RankedWord(string Word, int Rank)
{
    public override string ToString() => $"#{Rank}\t{Word}";
}
namespace MemeIndex_Core.Services.ImageAnalysis;

public interface IImageToTextService
{
    /// Processes image files.
    /// Subscribe to <see cref="ImageProcessed"/> event to obtain the results.
    /// <param name="paths"> Paths to the image files. </param>
    Task ProcessFiles(IEnumerable<string> paths);

    /// Returns a text representation of an image.
    /// <param name="path"> Path to the image. </param>
    Task<List<RankedWord>?> GetTextRepresentation(string path);

    public event Action<Dictionary<string, List<RankedWord>>>? ImageProcessed;
}

public delegate IImageToTextService ImageToTextServiceResolver(int key);

public record RankedWord(string Word, int Rank)
{
    public override string ToString() => $"#{Rank}\t{Word}";
}
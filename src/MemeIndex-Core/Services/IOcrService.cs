namespace MemeIndex_Core.Services;

public interface IOcrService
{
    /// <summary>
    /// Returns a text representation of an image.
    /// </summary>
    /// <param name="path">Filepath to the image.</param>
    /// <param name="lang">Language code.</param>
    Task<string?> GetTextRepresentation(string path, string lang);
}
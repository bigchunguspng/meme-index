using System.ComponentModel;

namespace MemeIndex_Core;

public class Config
{
    private string? _dataPath;

    [DefaultValue("data")]
    public string? DataPath
    {
        get => _dataPath;
        set
        {
            if (value == null) return;

            Directory.CreateDirectory(value);
            _dataPath = value;
        }
    }

    /// Use <see cref="GetDbConnectionString"/> method
    /// to get actual connection string.
    [DefaultValue(@"Data Source=[data][/]meme-index.db")]
    public string? DbConnectionString { get; set; }

    public string? GetDbConnectionString()
    {
        return DbConnectionString?
            .Replace("[data]", DataPath ?? string.Empty)
            .Replace("[/]", Path.DirectorySeparatorChar.ToString());
    }

    /// You can get one here: https://ocr.space/ocrapi/freekey
    [DefaultValue("helloworld")]
    public string? OrcApiKey { get; set; }
}
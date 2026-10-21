namespace MemeIndex.Tools.Logging;

// TODO - Use this for TASKS REPORTS (DEBUG) ?

/// Stores logs and write them to a file in batches (hardcoded to 32).
public class FileLogger_Batch(FilePath filePath)
{
    private readonly SemaphoreSlim    _s = new(1, 1);
    private readonly string?  _directory = filePath.DirectoryName;
    private readonly string[] _buffer    = new string[32];

    private int _head;

    /// Logs message. Triggers write every 32 calls.
    public async Task Log(string message) => await _s.Lock(async () =>
    {
        _buffer[_head] = message;
        await MoveHead();
    });

    /// Writes pending logs to file. Call this before exit!
    public async Task Write() => await _s.Lock(async () =>
    {
        await Write_Internal();
    });

    private async Task MoveHead()
    {
        if (++_head < 32) return;

        await Write_Internal();
    }

    private async Task Write_Internal()
    {
        _directory.CreateDirectory();
        await File.AppendAllLinesAsync(filePath, _buffer.Take(_head));
        _head = 0;
    }
}
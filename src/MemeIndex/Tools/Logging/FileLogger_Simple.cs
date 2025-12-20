namespace MemeIndex.Tools.Logging;

public class FileLogger_Simple(FilePath filePath)
{
    private readonly string?  _directory = filePath.DirectoryName;

    [MethodImpl(Synchronized)]
    public void Log(string message)
    {
        _directory.CreateDirectory();
        File.AppendAllText(filePath, message);
    }
}
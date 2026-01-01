using MemeIndex.Utils;

namespace MemeIndex.Core.OpeningFiles;

public abstract class FileOpener
{
    private static readonly FileOpener _instance
        = Helpers.IsWindows ? new WindowsFileOpener()
        : Helpers.IsLinux   ? new   LinuxFileOpener()
        :                     new     MacFileOpener();

    protected abstract string OpenFileTool { get; }
    protected abstract string ShowFileTool { get; }

    protected abstract string ShowFileArguments(string path);

    public static void OpenFileWithDefaultApp(string path)
    {
        using var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName  = _instance.OpenFileTool;
        process.StartInfo.Arguments = path.Quote();
        process.Start();
    }

    public static void ShowFileInExplorer(string path)
    {
        using var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName  = _instance.ShowFileTool;
        process.StartInfo.Arguments = _instance.ShowFileArguments(path);
        process.Start();
    }
}

public class LinuxFileOpener : FileOpener
{
    protected override string OpenFileTool => "xdg-open";
    protected override string ShowFileTool => "dbus-send";

    protected override string ShowFileArguments(string path) =>
        "--print-reply --dest=org.freedesktop.FileManager1 /org/freedesktop/FileManager1 " +
        "org.freedesktop.FileManager1.ShowItems array:string:\"file://" + path + "\" string:\"\"";
}

public class WindowsFileOpener : FileOpener
{
    protected override string OpenFileTool => "explorer";
    protected override string ShowFileTool => "explorer";

    protected override string ShowFileArguments(string path) => $"/select, {path.Quote()}";
}

public class MacFileOpener : FileOpener
{
    protected override string OpenFileTool => "explorer";
    protected override string ShowFileTool => "explorer";

    protected override string ShowFileArguments(string path) => $"-R {path.Quote()}";
}
using System.Diagnostics;
using MemeIndex_Core.Utils;

namespace MemeIndex_Gtk.Utils.FileOpener;

public abstract class FileOpener
{
    protected abstract string OpenFileTool { get; }
    protected abstract string ShowFileTool { get; }

    protected abstract string ShowFileArguments(string path);

    public void OpenFileWithDefaultApp(string path)
    {
        using var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = OpenFileTool;
        process.StartInfo.Arguments = path.Quote();
        process.Start();
    }

    public void ShowFileInExplorer(string path)
    {
        using var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = ShowFileTool;
        process.StartInfo.Arguments = ShowFileArguments(path);
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
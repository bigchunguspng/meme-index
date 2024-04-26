using System.Diagnostics;
using System.Runtime.InteropServices;
using MemeIndex_Core.Utils;

namespace MemeIndex_Gtk.Utils;

public static class FileOpener
{
    public static void OpenFileWithDefaultApp(string path)
    {
        using var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = IsLinux() ? "xdg-open" : "explorer";
        process.StartInfo.Arguments = path.Quote();
        process.Start();
    }

    public static void ShowFileInExplorer(string path)
    {
        using var process = new Process();

        var fullPath = path.Quote();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.FileName = IsLinux() ? "dbus-send" : "explorer";
        process.StartInfo.Arguments = IsLinux()
            ? GetLinuxArgs()
            : IsWindows()
                ? $"/select, {fullPath}"
                : $"-R {fullPath}";
        process.Start();

        string GetLinuxArgs() =>
            "--print-reply --dest=org.freedesktop.FileManager1 /org/freedesktop/FileManager1 " +
            "org.freedesktop.FileManager1.ShowItems array:string:\"file://" + path + "\" string:\"\"";
    }

    private static bool IsLinux  () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
using System.Runtime.InteropServices;

namespace MemeIndex_Gtk.Utils.FileOpener;

public static class FileOpenerFactory
{
    public static FileOpener GetFileOpener()
    {
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var osx     = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        return windows ? new WindowsFileOpener() : osx ? new MacFileOpener() : new LinuxFileOpener();
    }
}
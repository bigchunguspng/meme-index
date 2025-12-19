using MemeIndex.Utils;
using static System.Environment;

namespace MemeIndex.Core;

public static class Paths
{
    public static readonly FilePath
#if !DEVELOP
        Dir_AppData         = "data",
#else
        Dir_AppData         = new FilePath(SpecialFolder. LocalApplicationData).Combine(CLI.NAME),
#endif
        Dir_Common          = new FilePath(SpecialFolder.CommonApplicationData).Combine(CLI.NAME),

        //  WEB
        Dir_WebRoot         = "web", // WHEN changing - also change in MemeIndex.csproj!
        Dir_Thumbs          = Dir_WebRoot.Combine("thumb"),

        //   CONFIG
        File_Config         = GetConfigLocation().Combine("config.json"),
        File_Ports          = Dir_Common.Combine("ports.txt"),

        //  LOGS
        Dir_Logs            = Helpers.IsWindows ? Dir_AppData.Combine("logs") : $"/var/log/{CLI.NAME}",
        Dir_Traces          = Dir_Logs.Combine("traces"),

        //  DEBUG
        Dir_Debug           = Dir_AppData.Combine("debug-artifacts"),
        Dir_Debug_Color     = Dir_Debug.Combine("Color"),    // Color model visualizations.
        Dir_Debug_Profiles  = Dir_Debug.Combine("Profiles"), // Image -> plot  + data.s
        Dir_Debug_Image     = Dir_Debug.Combine("Image"),    // Image -> image + data.
        Dir_Debug_Mixed     = Dir_Debug.Combine("Mixed");    // Image -> image + data + plot.

    private static FilePath GetConfigLocation() => Helpers.IsWindows
        ? Dir_AppData
        : new FilePath(SpecialFolder.ApplicationData);
}

// TODO - is this user or system app?
/*
LINUX:
    /bin/memeindex ->
    /usr/lib/memeindex
        libe_sqlite3.so
        memeindex
    /usr/share/memeindex/web/*
    /var/log/memeindex/*
    ~/.config/memeindex/config.json
    ~/.local/share/memeindex/
        meme-index.db
WINDOWS:
    C:\Program Files\MemeIndex\
        web\*
        e_sqlite3.dll
        MemeIndex.exe
    ~\AppData\Local\MemeIndex\
        logs\*
        config.json
        meme-index.db
*/

/* == NOTES:

CommonApplicationData                       /usr/share          "C:\ProgramData"
ApplicationData         $XDG_CONFIG_HOME    ~/.config           "~\AppData\Roaming"
LocalApplicationData    $XDG_DATA_HOME      ~/.local/share      "~\AppData\Local"
Path.GetTempPath()                          /tmp                "~\AppData\Local\Temp\"

*/
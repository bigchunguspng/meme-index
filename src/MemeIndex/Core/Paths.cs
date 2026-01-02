using MemeIndex.Utils;
using static System.Environment;

namespace MemeIndex.Core;

public static class Paths
{
    public static readonly FilePath
#if DEVELOP
        Dir_AppData         = new FilePath(CurrentDirectory).Combine("data"),
#else
        Dir_AppData         = new FilePath(SpecialFolder. LocalApplicationData).Combine(CLI.NAME),
#endif
        Dir_Common          = new FilePath(SpecialFolder.CommonApplicationData).Combine(CLI.NAME),

        //  WEB
        Dir_WebRoot         = "web", // WHEN changing - also change in MemeIndex.csproj!

        //  THUMBS
        Dir_Thumbs          = Dir_AppData.Combine("thumbs"),
        Dir_Thumbs_WEB      = "/thumb",

        //   CONFIG
        File_Config         = GetConfigLocation().Combine("config.json"),
        File_Ports          = Dir_Common.Combine("ports.txt"),

        //  LOGS
        Dir_Logs            = Helpers.IsWindows ? Dir_AppData.Combine("logs") : $"/var/log/{CLI.NAME}",
        Dir_Traces          = Dir_Logs.Combine("traces"),
        File_Log            = Dir_Logs.Combine("log.txt"),
        File_Err            = Dir_Logs.Combine("err.txt"),

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

// TODO implement scheme below
/*
LINUX:
    /bin/                       // == FOR ALL USERS, readonly, on PATH
        memeindex ->
    /usr/lib/memeindex/         // == FOR ALL USERS, readonly, architecture-__dependent
        memeindex
        libe_sqlite3.so
        web/ ->
    /usr/share/memeindex/       // == FOR ALL USERS, readonly, architecture-independent
        web/*
    /var/lib/memeindex/         // == FOR ALL USERS, runtime-writen
        ports.txt
    ~/.local/share/memeindex/   // == FOR ONE USER,  runtime-writen, data
        debug-artifacts/*
        thumbs/*.webp
        web/*
        meme-index.db
    ~/.config/memeindex/        // == FOR ONE USER,  runtime-writen, config
        config.json
    ~/.cache/memeindex/         // == FOR ONE USER,  runtime-writen, disposable
        logs/*
WINDOWS:
    C:\Program Files\MemeIndex\ // == FOR ALL USERS, readonly
        web\*                   // frontend files, statically hosted
        e_sqlite3.dll           //  backend libs
        MemeIndex.exe           //  backend executable
    C:\ProgramData\MemeIndex\   // == FOR ALL USERS, runtime-writen
        ports.txt               // mapping users to http ports,
    ~\AppData\Local\MemeIndex\  // == FOR ONE USER,  runtime-writen
        debug-artifacts\*       // for development purpose
        logs\*                  // logs
        thumbs\*.webp           // thumbnails,     statically hosted
        web\*                   // frontend files, statically hosted, alternative [^1]
        config.json             // user config
        meme-index.db           // user db
DEVELOPMENT:
    …\out\bin\…
        MemeIndex.exe
        e_sqlite3.dll
        web\*
        data\
            debug-artifacts\*
            logs\*
            thumbs\*.webp
            config.json
            meme-index.db
            ports.txt

[^1]: user can configure app to use another directory for frontend content.
*/

/* == NOTES:

CommonApplicationData                       /usr/share          "C:\ProgramData"
ApplicationData         $XDG_CONFIG_HOME    ~/.config           "~\AppData\Roaming"
LocalApplicationData    $XDG_DATA_HOME      ~/.local/share      "~\AppData\Local"
Path.GetTempPath()                          /tmp                "~\AppData\Local\Temp\"

*/
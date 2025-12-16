namespace MemeIndex.Core;

public static class Paths
{
    public static readonly FilePath
#if DEVELOP
        Dir_AppData         = new ("MemeIndex"),
#else
        Dir_AppData         = new FilePath(Environment.SpecialFolder.LocalApplicationData).Combine("MemeIndex"),
#endif
        Dir_Logs            = Dir_AppData.Combine("logs"),
        Dir_Debug           = Dir_AppData.Combine("debug-artifacts"),
        File_Config         = Dir_AppData.Combine("config.json"),
        //
        Dir_WebRoot         = new ("web"), // change? also change in .csproj!
        Dir_Thumbs          = Dir_WebRoot.Combine("thumb"),
        //
        Dir_Traces          = Dir_Logs.Combine("traces"),
        //
        Dir_Debug_Color     = Dir_Debug.Combine("Color"),    // Color model visualizations.
        Dir_Debug_Profiles  = Dir_Debug.Combine("Profiles"), // Image -> plot  + data.s
        Dir_Debug_Image     = Dir_Debug.Combine("Image"),    // Image -> image + data.
        Dir_Debug_Mixed     = Dir_Debug.Combine("Mixed");    // Image -> image + data + plot.
}

/* == NOTES:

CommonApplicationData                       /usr/share          "C:\ProgramData"
ApplicationData         $XDG_CONFIG_HOME    ~/.config           "~\AppData\Roaming"
LocalApplicationData    $XDG_DATA_HOME      ~/.local/share      "~\AppData\Local"
Path.GetTempPath()                          /tmp                "~\AppData\Local\Temp\"

*/
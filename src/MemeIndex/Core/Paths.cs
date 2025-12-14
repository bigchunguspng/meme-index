namespace MemeIndex.Core;

public static class Paths
{
    public static readonly FilePath
        Dir_WebRoot         = new("web"), // also change in .csproj!
        Dir_Thumbs          = Dir_WebRoot.Combine("thumb"),
        Dir_Logs            = new("logs"),
        Dir_Traces          = Dir_Logs.Combine("traces"),
        Dir_Debug           = new("Debug-Artifacts"),
        Dir_Debug_Color     = Dir_Debug.Combine("Color"),    // Color model visualizations.
        Dir_Debug_Profiles  = Dir_Debug.Combine("Profiles"), // Image -> plot  + data.
        Dir_Debug_Image     = Dir_Debug.Combine("Image"),    // Image -> image + data.
        Dir_Debug_Mixed     = Dir_Debug.Combine("Mixed");    // Image -> image + data + plot.
}
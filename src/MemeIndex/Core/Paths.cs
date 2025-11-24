namespace MemeIndex.Core;

public static class Paths
{
    public static readonly FilePath
        Dir_Debug           = new("Debug-Artifacts"),
        Dir_Debug_Color     = Dir_Debug.Combine("Color"),    // Color model visualizations.
        Dir_Debug_Profiles  = Dir_Debug.Combine("Profiles"), // Image -> plot  + data.
        Dir_Debug_Image     = Dir_Debug.Combine("Image");    // Image -> image + data.
}
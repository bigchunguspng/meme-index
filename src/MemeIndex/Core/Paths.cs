namespace MemeIndex.Core;

public static class Paths
{
    public static readonly FilePath
        Dir_Debug               = new("Debug-Artifacts"),
        Dir_Debug_HSL           = Dir_Debug.Combine("HSL"),
        Dir_Debug_HSL_Profiles  = Dir_Debug.Combine("HSL-Profiles"),
        Dir_Debug_SampleGrids   = Dir_Debug.Combine("Sample-Grids");
}
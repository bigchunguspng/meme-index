namespace MemeIndex.Tools.Backrooms.Helpers;

public static class SystemHelpers
{
    private static bool? AOT;

    public static bool IsAOT()
    {
#pragma warning disable IL2026
        return AOT ??= new StackTrace(false).GetFrame(0)?.GetMethod() is null;
#pragma warning restore IL2026
    }
}
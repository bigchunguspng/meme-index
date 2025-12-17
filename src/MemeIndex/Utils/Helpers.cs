using System.Runtime.InteropServices;

namespace MemeIndex.Utils;

public static class Helpers
{
    public const string COMPILE_MODE =
#if AOT
        "AOT";
#else
        "JIT";
#endif

    public static readonly bool IsWindows =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
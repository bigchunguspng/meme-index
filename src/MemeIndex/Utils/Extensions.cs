namespace MemeIndex.Utils;

public static class Extensions // App specific! Reusables go to Tools/
{
    public const long
        KB =      1024,
        MB = KB * 1024,
        GB = MB * 1024;

    public static ReadOnlySpan<char> FileSize_Format(this long size) => size switch
    {
        < KB => $"{size} B",
        < MB => $"{size / (double)KB:0.0} KB",
        < GB => $"{size / (double)MB:0.0} MB",
        _    => $"{size / (double)GB:0.0} GB",
    };
}
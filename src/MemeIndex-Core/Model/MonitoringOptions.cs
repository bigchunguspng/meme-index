namespace MemeIndex_Core.Model;

public record MonitoringOptions(string Path, bool Recursive, HashSet<int> Means)
{
    public static HashSet<int> DefaultMeans => new() { 1, 2 };
}
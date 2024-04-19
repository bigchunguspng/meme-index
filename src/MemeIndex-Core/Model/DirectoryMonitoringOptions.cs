namespace MemeIndex_Core.Model;

public record DirectoryMonitoringOptions(string Path, bool Recursive, HashSet<int> Means);
using MemeIndex_Core.Data;

namespace MemeIndex_Core.Model;

public record MonitoringOptions(string Path, bool Recursive, HashSet<int> Means)
{
    public static HashSet<int> DefaultMeans => new() { 1, 2 };

    public bool IsTheSameAs(MonitoringOptions other)
    {
        return Path == other.Path
            && Recursive == other.Recursive
            && Means.Count == other.Means.Count
            && !Means.Except(other.Means).Any()
            && !other.Means.Except(Means).Any();
    }

    public class MeansBuilder
    {
        private readonly HashSet<int> _means = new();

        public MeansBuilder WithRgb(bool condition = true)
        {
            if (condition) _means.Add(DatabaseInitializer.RGB_CODE);
            return this;
        }

        public MeansBuilder WithEng(bool condition = true)
        {
            if (condition) _means.Add(DatabaseInitializer.ENG_CODE);
            return this;
        }

        public HashSet<int> Build() => _means;
    }
}
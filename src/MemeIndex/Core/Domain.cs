namespace MemeIndex.Core;

public record RankedWord(string Word, int Rank)
{
    public override string ToString() => $"#{Rank}\t{Word}";
}

public record TagContent(string Term, int Score);

// put entities to be used in app here
// put db specific types in DB/

public record FilePathRecord(int Id, string Path);
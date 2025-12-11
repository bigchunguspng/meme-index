namespace MemeIndex.Core;

public record RankedWord(string Word, int Rank)
{
    public override string ToString() => $"#{Rank}\t{Word}";
}

public record TagContent(string Term, int Score);
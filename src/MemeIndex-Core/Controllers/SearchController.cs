using MemeIndex_Core.Entities;

namespace MemeIndex_Core.Controllers;

public class SearchController
{
    // (just for now):

    //private Dictionary<string, List<Entities.File>> _cache;

    public void Search(IEnumerable<SearchOption> options, LogicalOperator @operator)
    {
        throw new NotImplementedException();
    }

    public void SearchAll(SearchRequestItem word)
    {
        throw new NotImplementedException();
    }

    public void SearchCached(SearchRequestItem word)
    {
        throw new NotImplementedException();
    }
}

public record SearchRequestItem(Mean Mean, string Word);

public record SearchOption(Mean Mean, IEnumerable<string> Words, LogicalOperator Operator);

public enum LogicalOperator
{
    OR,
    AND
}
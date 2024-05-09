using MemeIndex_Core.Controllers;

namespace MemeIndex_Core.Objects;

public class SearchQuery
{
    public SearchQuery(int meanId, List<string> words, LogicalOperator @operator)
    {
        MeanId = meanId;
        Words = words;
        Operator = @operator;
    }

    public int             MeanId   { get; }
    public List<string>    Words    { get; }
    public LogicalOperator Operator { get; }
}
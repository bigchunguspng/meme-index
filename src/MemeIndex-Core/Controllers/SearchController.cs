using MemeIndex_Core.Objects;
using MemeIndex_Core.Services.Search;
using MemeIndex_Core.Utils;
using File = MemeIndex_Core.Data.Entities.File;

namespace MemeIndex_Core.Controllers;

public class SearchController
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<HashSet<File>> Search(IEnumerable<SearchQuery> queries, LogicalOperator @operator)
    {
        var sw = Helpers.GetStartedStopwatch();
        var files = new Dictionary<File, double>();
        foreach (var query in queries)
        {
            var filesByQuery = new Dictionary<File, double>();
            foreach (var word in query.Words)
            {
                var item = new SearchRequestItem(query.MeanId, word, SearchStrategyByMeanId(query.MeanId));
                var results = await _searchService.FindAll(item);
                JoinResults(filesByQuery, results, query.Operator);
            }

            JoinResults(files, filesByQuery, @operator);
        }

        var result = files
            .OrderByDescending(x => x.Value)
            .Select(g => g.Key)
            .ToHashSet();

        sw.Log($"[Search / {result.Count} files]");
        return result;
    }

    private static void JoinResults
    (
        Dictionary<File, double> first,
        Dictionary<File, double> second,
        LogicalOperator @operator
    )
    {
        var unite = @operator == LogicalOperator.OR || first.Count == 0 || second.Count == 0;
        if (unite)
        {
            foreach (var pair in second)
            {
                if (first.ContainsKey(pair.Key))
                    first[pair.Key] *= pair.Value;
                else
                    first.Add(pair.Key, pair.Value);
            }
        }
        else
        {
            foreach (var pair in second)
            {
                if (first.ContainsKey(pair.Key))
                    first[pair.Key] *= pair.Value;
            }

            var keys = first.Where(x => !second.ContainsKey(x.Key)).Select(x => x.Key);
            foreach (var key in keys)
            {
                first.Remove(key);
            }
        }
    }

    private static SearchStrategy SearchStrategyByMeanId(int meanId) => meanId switch
    {
        1 => SearchStrategy.EQUALS_ONLY,
        _ => SearchStrategy.EQUALS_AND_CONTAINS
    };
}

public record SearchRequestItem(int MeanId, string Word, SearchStrategy Strategy);

public enum LogicalOperator
{
    OR,
    AND,
}
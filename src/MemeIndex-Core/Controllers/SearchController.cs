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
        var filesAll = new HashSet<File>();
        foreach (var query in queries)
        {
            var filesByQuery = new HashSet<File>();
            foreach (var word in query.Words)
            {
                var item = new SearchRequestItem(query.MeanId, word, SearchStrategyByMeanId(query.MeanId));
                var result = await _searchService.FindAll(item);
                JoinResults(filesByQuery, result, query.Operator);
            }

            JoinResults(filesAll, filesByQuery, @operator);
        }

        return filesAll;
    }

    private static void JoinResults<T>(ISet<T> accumulator, ICollection<T> addition, LogicalOperator @operator)
    {
        var unite = @operator == LogicalOperator.OR || accumulator.Count == 0 || addition.Count == 0;
        unite.Switch(accumulator.UnionWith, accumulator.IntersectWith)(addition);
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
using MemeIndex_Core.Data;
using MemeIndex_Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace MemeIndex_Core.Controllers;

public class SearchController
{
    //private Dictionary<string, List<Entities.File>> _cache;

    private readonly MemeDbContext _context;

    public SearchController(MemeDbContext context)
    {
        _context = context;
    }

    public async Task<HashSet<Entities.File>> Search(IEnumerable<SearchQuery> queries, LogicalOperator @operator)
    {
        var filesAll = new HashSet<Entities.File>();
        foreach (var query in queries)
        {
            var filesByQuery = new HashSet<Entities.File>();
            foreach (var word in query.Words)
            {
                var item = new SearchRequestItem(query.MeanId, word, SearchStrategyByMeanId(query.MeanId));
                var result = await SearchAll(item);
                JoinResults(filesByQuery, result, query.Operator);
            }

            JoinResults(filesAll, filesByQuery, @operator);
        }

        return filesAll;
    }

    public async Task<List<Entities.File>> SearchAll(SearchRequestItem item)
    {
        var tagsByMean = _context.Tags
            .Include(x => x.Word)
            .Include(x => x.File)
            .ThenInclude(x => x.Directory)
            .Where(x => x.MeanId == item.MeanId);

        var tagsFiltered = item.Strategy switch
        {
            SearchStrategy.EQUALS_ONLY
                => tagsByMean.Where(x => x.Word.Text == item.Word),
            SearchStrategy.EQUALS_AND_START
                => tagsByMean.Where(x => x.Word.Text == item.Word || EF.Functions.Like(x.Word.Text, $"{item.Word}%")),
            SearchStrategy.EQUALS_AND_END
                => tagsByMean.Where(x => x.Word.Text == item.Word || EF.Functions.Like(x.Word.Text, $"%{item.Word}")),
            _
                => tagsByMean.Where(x => x.Word.Text == item.Word || EF.Functions.Like(x.Word.Text, $"%{item.Word}%")),
        };

        var files = await tagsFiltered
            .GroupBy(x => x.File)
            .OrderBy(g => g.Count())
            .ThenBy(g => g.Min(x => x.Rank))
            .Select(g => g.Key)
            .ToListAsync();

        return files;
    }

    public void SearchCached(SearchRequestItem word)
    {
        throw new NotImplementedException();
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

public record SearchQuery(int MeanId, List<string> Words, LogicalOperator Operator);

public enum LogicalOperator
{
    OR,
    AND,
}

public enum SearchStrategy
{
    EQUALS_ONLY,
    EQUALS_AND_START,
    EQUALS_AND_END,
    EQUALS_AND_CONTAINS,
}
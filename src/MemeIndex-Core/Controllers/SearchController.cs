using MemeIndex_Core.Data;
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

    public void Search(IEnumerable<SearchQuery> queries, LogicalOperator @operator)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Entities.File>> SearchAll(SearchRequestItem item)
    {
        var files = await _context.Tags
            .Include(x => x.Word)
            .Include(x => x.File)
            .ThenInclude(x => x.Directory)
            .Where(x => x.MeanId == item.MeanId)
            .Where(x => x.Word.Text == item.Word || EF.Functions.Like(x.Word.Text, $"{item.Word}%"))
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
}

public record SearchRequestItem(int MeanId, string Word);

public record SearchQuery(int MeanId, IEnumerable<string> Words, LogicalOperator Operator);

public enum LogicalOperator
{
    OR,
    AND
}
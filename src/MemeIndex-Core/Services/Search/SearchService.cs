using MemeIndex_Core.Controllers;
using MemeIndex_Core.Data;
using Microsoft.EntityFrameworkCore;
using File = MemeIndex_Core.Entities.File;

namespace MemeIndex_Core.Services.Search;

public class SearchService
{
    private readonly MemeDbContext _context;

    public SearchService(MemeDbContext context)
    {
        _context = context;
    }

    public async Task<List<File>> FindAll(SearchRequestItem item)
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
}

public enum SearchStrategy
{
    EQUALS_ONLY,
    EQUALS_AND_START,
    EQUALS_AND_END,
    EQUALS_AND_CONTAINS,
}
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using SixLabors.ImageSharp;

namespace MemeIndex.Core.Search;

public static class Jarvis
{
    public static async Task<SearchResponse> Search(string[] terms)
    {
        var sw = Stopwatch.StartNew();
        const int TAKE = 100;
        var con  = await AppDB.ConnectTo_Main();
        sw.Log("db connect");
        var tags = await con.Tags_GetByTerms(terms);
        sw.Log("get tags");

        var tags_byFile = tags
            .GroupBy(x => x.file_id)
            .Where(g => g.Count() == terms.Length);
        // ^ remove files where some tags are missing.

        var score_byFile = tags_byFile
            .Select(g => new { FileId = g.Key, Score = CalculateScore(g.Select(x => x.score)) })
            .OrderByDescending(x => x.Score)
            .Take(TAKE)
            .ToDictionary(x => x.FileId, x => x.Score);
        sw.Log("get score by file");

        var files_db = await con.Files_UI_GetByIds(score_byFile.Keys);
        sw.Log("get files");
        var files = files_db.ToArray();
        sw.Log($"files to array -> {files.Length}");
        var dirs = await con.Dirs_GetByIds(files.Select(x => x.dir_id).Distinct());
        sw.Log("get dirs");
        return new SearchResponse
        {
            p = new Pagination(0, files.Length, -1),
            d = dirs.ToDictionary(x => x.Id, x => x.Path + Path.DirectorySeparatorChar),
            f = files
                .OrderByDescending(x => score_byFile[x.id])
                .Select(x => new File_UI(x)),
        };
    }

    private static int CalculateScore(IEnumerable<int> scores)
    {
        var result = 1.0;
        foreach (var score in scores)
        {
            result *= (double)score / ColorTagger_v2.MAX_SCORE;
        }

        return (int)(result * ColorTagger_v2.MAX_SCORE);
    }
}

public record struct Pagination(int o, int r, int t); // offset, returned, total

public class SearchResponse
{
    public required Pagination              p { get; set; }
    public required Dictionary<int, string> d { get; set; }
    public required IEnumerable   <File_UI> f { get; set; }
}

public class File_UI(DB_File_UI file)
{
    public int      i { get; } = file.id;
    public int      d { get; } = file.dir_id;
    public string   n { get; } = file.name;
    public long     s { get; } = file.size;
    public DateTime m { get; } = DateTime.FromFileTimeUtc(file.mdate);
    public Size?    x { get; } = file is { image_w: not null, image_h: not null }
        ? new Size(file.image_w.Value, file.image_h.Value)
        : null;
}
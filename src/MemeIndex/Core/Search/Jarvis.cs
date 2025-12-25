using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using SixLabors.ImageSharp;

namespace MemeIndex.Core.Search;

public static class Jarvis
{
    public static async Task<IEnumerable<File_UI>> Search(string[] terms)
    {
        const int TAKE = 100;
        var con  = await AppDB.ConnectTo_Main();
        var tags = await con.Tags_GetByTerms(terms);

        var tags_byFile = tags
            .GroupBy(x => x.file_id)
            .Where(g => g.Count() == terms.Length);
        // ^ remove files where some tags are missing.

        var score_byFile = tags_byFile
            .Select(g => new { FileId = g.Key, Score = CalculateScore(g.Select(x => x.score)) })
            .OrderByDescending(x => x.Score)
            .Take(TAKE)
            .ToDictionary(x => x.FileId, x => x.Score);

        var files = await con.Files_UI_ByIds(score_byFile.Keys);
        var files_sorted = files.OrderByDescending(x => score_byFile[x.id]);
        return files_sorted.Select(x => new File_UI(x));
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

public class File_UI(DB_File_UI file)
{
    public int      Id { get; } = file.id;
    public string Path { get; } = System.IO.Path.Combine(file.path, file.name);
    public string Webp { get; } = $"{Dir_Thumbs_WEB}/{file.id:x6}.webp";
    public long   Size { get; } = file.size;
    public DateTime DM { get; } = DateTime.FromFileTimeUtc(file.mdate);
    public Size? Image { get; } = file is { image_w: not null, image_h: not null }
        ? new Size(file.image_w.Value, file.image_h.Value)
        : null;
}
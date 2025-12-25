using Dapper;
using MemeIndex.Core;
using Microsoft.Data.Sqlite;

namespace MemeIndex.DB;

public class DB_Tag
{
    public int    file_id { get; set; }
    public string term    { get; set; } = null!;
    public int    score   { get; set; }
}

public class DB_Tag_Insert(TagContent tag, int file_id)
{
    public readonly int    file_id = file_id;
    public readonly string term    = tag.Term;
    public readonly int    score   = tag.Score;
}

public static class DB_Tags
{
    public static async Task Tags_CreateMany
        (this SqliteConnection c, IEnumerable<DB_Tag_Insert> tags)
    {
        const string SQL =
            "INSERT OR IGNORE "
          + "INTO tags (file_id, term, score) "
          + "VALUES (@file_id, @term, @score)";
        await using var transaction = c.BeginTransaction();
        await c.ExecuteAsync(SQL, tags, transaction);
        await transaction.CommitAsync();
    }

    public static async Task<IEnumerable<DB_Tag>> Tags_GetByTerms
        (this SqliteConnection c, string[] terms)
    {
        // todo - add anti sql injection measures since we can't use params
        var SQL
            = "SELECT file_id, term, score "
            + "FROM tags "
            + $"WHERE term IN ({string.Join(',', terms.Select(x => $"'{x}'"))})";
        return await c.QueryAsync<DB_Tag>(SQL);
    }
}
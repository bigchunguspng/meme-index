using Dapper;
using MemeIndex.Core;
using Microsoft.Data.Sqlite;

namespace MemeIndex.DB;

public class BD_Tag_Insert(TagContent tag, int file_id)
{
    public readonly int    file_id = file_id;
    public readonly string term    = tag.Term;
    public readonly int    score   = tag.Score;
}

public static class DB_Tags
{
    public static async Task Tags_CreateMany
        (this SqliteConnection c, IEnumerable<BD_Tag_Insert> tags)
    {
        const string SQL =
            "INSERT OR IGNORE "
          + "INTO tags (file_id, term, score) "
          + "VALUES (@file_id, @term, @score)";
        await c.ExecuteAsync(SQL, tags);
    }

}
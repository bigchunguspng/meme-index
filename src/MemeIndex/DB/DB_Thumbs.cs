using Dapper;
using Microsoft.Data.Sqlite;

namespace MemeIndex.DB;

public struct DB_Thumb_Insert
    (int file_id, byte[] data)
{
    public readonly int    file_id = file_id;
    public readonly byte[] thumb   = data;
}

public static class DB_Thumbs
{
    // CREATE

    public static async Task Thumb_CreateOrUpdate
        (this SqliteConnection c, DB_Thumb_Insert thumb, SqliteTransaction? tx = null)
    {
        const string SQL =
            "INSERT OR REPLACE "
          + "INTO thumbs (file_id, thumb) "
          + "VALUES (@file_id, @thumb)";

        await using var command = c.CreateCommand();

        // yes, byte[] doesn't work normal way

        command.CommandText = SQL;
        command.Parameters.Add("@file_id", SqliteType.Integer).Value = thumb.file_id;
        command.Parameters.Add("@thumb",   SqliteType.Blob   ).Value = thumb.thumb;
        command.Transaction = tx;

        await command.ExecuteNonQueryAsync();
    }

    // GET

    public static async Task<byte[]> Thumb_Get
        (this SqliteConnection c, int id)
    {
        const string SQL = "SELECT thumb FROM thumbs WHERE file_id = @id";
        return await c.QuerySingleOrDefaultAsync<byte[]>(SQL, new { id }) ?? [];
    }

    // UPDATE

    // DELETE

    public static async Task Thumbs_Delete
        (this SqliteConnection c, SqliteTransaction? transaction = null)
    {
        const string SQL = "DELETE FROM thumbs";
        await c.ExecuteAsync(SQL, null, transaction);
    }
}
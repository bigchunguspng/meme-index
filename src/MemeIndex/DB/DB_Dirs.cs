using Dapper;
using Microsoft.Data.Sqlite;

namespace MemeIndex.DB;

public class DB_Dir_Get
{
    public required int    id;
    public required string path;
}

public static class DB_Dirs
{
    // CREATE

    public static async Task Dirs_Create
        (this SqliteConnection c, string path)
    {
        const string SQL = "INSERT INTO dirs (path) VALUES (@path)";
        await c.ExecuteAsync(SQL, new { path });
    }

    public static async Task Dirs_CreateMany
        (this SqliteConnection c, IEnumerable<string> paths)
    {
        const string SQL = "INSERT OR IGNORE INTO dirs (path) VALUES (@path)";
        var insert = paths.Select(x => new { path = x });
        await using var transaction = c.BeginTransaction();
        await c.ExecuteAsync(SQL, insert, transaction);
        await transaction.CommitAsync();
    }

    // GET

    public static async Task<IEnumerable<DB_Dir_Get>> Dirs_GetAll
        (this SqliteConnection c)
    {
        const string SQL = "SELECT * FROM dirs";
        return await c.QueryAsync<DB_Dir_Get>(SQL);
    }

    public static async Task<IEnumerable<DB_Dir_Get>> Dirs_GetByIds
        (this SqliteConnection c, IEnumerable<int> ids)
    {
        // todo anti-injection
        var SQL = $"SELECT * FROM dirs WHERE id IN ({string.Join(',', ids)})";
        return await c.QueryAsync<DB_Dir_Get>(SQL);
    }

    public static async Task<DB_Dir_Get> Dirs_GetByPath
        (this SqliteConnection c, string path)
    {
        const string SQL = "SELECT * FROM dirs WHERE path = @path";
        return await c.QuerySingleAsync<DB_Dir_Get>(SQL, new { path });
    }

    // UPDATE
    // DELETE
}
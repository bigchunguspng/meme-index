using Dapper;
using Microsoft.Data.Sqlite;

namespace MemeIndex.DB;

public class DB_Dir(FilePath path)
{
    public int    Id   { get; set; }
    public string Path { get; set; } = path;
}

public static class DB_Dirs
{
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
        await c.ExecuteAsync(SQL, paths.Select(x => new { path = x }));
    }
    
    public static async Task<IEnumerable<DB_Dir>> Dirs_GetAll
        (this SqliteConnection c)
    {
        const string SQL = "SELECT * FROM dirs";
        return await c.QueryAsync<DB_Dir>(SQL);
    }
}
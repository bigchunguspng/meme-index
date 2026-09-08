using Dapper;
using Microsoft.Data.Sqlite;

namespace MemeIndex.DB;

public class DB_Monitor_Get
{
    public required int    id;
    public required int    dir_id;
    public required int    method;
    public required bool   recurse;
    public required bool   enabled;
    public required string path;
}

public class DB_Monitor_Insert(int dir_id, int method, bool recurse, bool enabled)
{
    public readonly int  dir_id  = dir_id;
    public readonly int  method  = method;
    public readonly bool recurse = recurse;
    public readonly bool enabled = enabled;
}

public class DB_Monitor_Update(int id, bool recurse, bool enabled)
{
    public readonly int  id      = id;
    public readonly bool recurse = recurse;
    public readonly bool enabled = enabled;
}

public static class DB_Monitors
{
    // CREATE

    public static async Task Monitors_CreateMany
        (this SqliteConnection c, SqliteTransaction? transaction, IEnumerable<DB_Monitor_Insert> monitors)
    {
        const string SQL =
            "INSERT OR IGNORE "
            + "INTO monitors (dir_id, method, recurse, enabled) "
            + "VALUES (@dir_id, @method, @recurse, @enabled)";
        await c.ExecuteAsync(SQL, monitors, transaction);
    }

    // GET

    public static async Task<List<DB_Monitor_Get>> Monitors_GetAll
        (this SqliteConnection c)
    {
        const string SQL =
            "SELECT monitors.*, dirs.path "
          + "FROM monitors "
          + "JOIN dirs ON dirs.id = dir_id";
        return await c.QueryAsync<DB_Monitor_Get>(SQL) as List<DB_Monitor_Get> ?? [];
    }

    // UPDATE

    public static async Task Monitor_Update
        (this SqliteConnection c, SqliteTransaction? transaction, DB_Monitor_Update monitor)
    {
        const string SQL =
            "UPDATE monitors "
          + "SET recurse = @recurse, enabled = @enabled "
          + "WHERE id = @id";
        await c.ExecuteAsync(SQL, monitor, transaction);
    }

    // DELETE

    public static async Task Monitor_Delete
        (this SqliteConnection c, SqliteTransaction? transaction, int id)
    {
        const string SQL = "DELETE FROM monitors WHERE id = @id";
        await c.ExecuteAsync(SQL, new { id }, transaction);
    }
}
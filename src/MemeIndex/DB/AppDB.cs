using Dapper;
using Microsoft.Data.Sqlite;

[module: DapperAot] // https://aot.dapperlib.dev/rules/DAP005

namespace MemeIndex.DB;

public static class AppDB
{
    // MAIN

    private static string?  _DB_Path_Main;
    private static string    DB_Path_Main => _DB_Path_Main ??= GetDB_Path_Main();
    private static string GetDB_Path_Main
        () => new FilePath("data-v2").EnsureDirectoryExist().Combine("meme-index.db");

    public static Task<SqliteConnection> ConnectTo_Main
        () => OpenConnection(DB_Path_Main);

    public static async Task CreateDB_Main
        (this SqliteConnection connection)
    {
        await connection.ExecuteAsync(_SQL_PRAGMAS_PER_DB);
        await connection.ExecuteAsync(_SQL_CREATE_TABLES_MAIN);
    }

    // RAW

    private static string?  _DB_Path_Raw;
    private static string    DB_Path_Raw => _DB_Path_Raw ??= GetDB_Path_Raw();
    private static string GetDB_Path_Raw
        () => new FilePath("data-v2").EnsureDirectoryExist().Combine("raw.db");

    public static Task<SqliteConnection> ConnectTo_Raw
        () => OpenConnection(DB_Path_Raw);

    public static async Task CreateDB_Raw
        (this SqliteConnection connection)
    {
        await connection.ExecuteAsync(_SQL_PRAGMAS_PER_DB);
        await connection.ExecuteAsync(_SQL_CREATE_TABLES_RAW);
    }

    //

    private static async Task<SqliteConnection> OpenConnection(string db_path)
    {
        var connection = new SqliteConnection($"Data Source={db_path}");
        await connection.OpenAsync();
        return connection;
    }

    private const string
        _SQL_PRAGMAS_PER_DB =
            "PRAGMA journal_mode = WAL;",
        _SQL_CREATE_TABLES_MAIN =
            """
            CREATE TABLE IF NOT EXISTS dirs
            (
                id      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                path    TEXT    NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS monitors
            (
                id      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                dir_id  INTEGER NOT NULL,
                method  INTEGER NOT NULL,
                recurse INTEGER NOT NULL DEFAULT 1,
                enabled INTEGER NOT NULL DEFAULT 1,
                UNIQUE (dir_id, method),
                FOREIGN KEY (dir_id)
                REFERENCES dirs (id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS files
            (
                id      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                dir_id  INTEGER NOT NULL,
                name    TEXT    NOT NULL,
                size    INTEGER NOT NULL,
                cdate   INTEGER NOT NULL,
                mdate   INTEGER NOT NULL,
                adate   INTEGER,
                tdate   INTEGER,
                image_w INTEGER,
                image_h INTEGER,
                UNIQUE (dir_id, name),
                FOREIGN KEY (dir_id)
                REFERENCES dirs (id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS tags
            (
                id      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                file_id INTEGER NOT NULL,
                term    TEXT    NOT NULL,
                score   INTEGER NOT NULL,
                UNIQUE (file_id, term),
                FOREIGN KEY (file_id)
                REFERENCES files (id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS files_broken
            (
                file_id INTEGER NOT NULL,
                FOREIGN KEY (file_id)
                REFERENCES files (id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_tags_term
            ON tags (term);
            """,
        _SQL_CREATE_TABLES_RAW =
            """
            CREATE TABLE IF NOT EXISTS analysis
            (
                file_id INTEGER NOT NULL UNIQUE,
                data    TEXT    NOT NULL
            );
            """;
}
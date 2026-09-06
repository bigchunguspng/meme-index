using Dapper;
using MemeIndex.Core;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;

namespace MemeIndex.DB;

public class DB_File_Get_UI
{
    public required int      id;
    public required int  dir_id;
    public required string name;
    public required long   size;
    public required long   mdate;
    public required int?   image_w;
    public required int?   image_h;
    public required double sort;
}

public class DB_File_Get_UI_WithCount : DB_File_Get_UI
{
    public required int    total;
}

public class DB_File_Get_ForSync
{
    public required int      id;
    public required int  dir_id;
    public required string name;
    public required long   size;
    public required long   cdate;
    public required long   mdate;
}

public class DB_File_Get_WithPath
{
    public required int    id;
    public required string path;
    public required string name;

    public string GetPath() => Path.Combine(path, name);

    public FilePathRecord Compile() => new (id, GetPath());
}

public class DB_File_Insert(FileInfo info, int directory_id)
{
    public readonly int    dir_id = directory_id;
    public readonly string name   = info.Name;
    public readonly long   size   = info.Length;
    public readonly long   cdate  = info. CreationTimeUtc.ToFileTimeUtc();
    public readonly long   mdate  = info.LastWriteTimeUtc.ToFileTimeUtc();
}

public class DB_File_Update(int file_id, int directory_id, FileInfo file)
{
    public readonly int    id     = file_id;
    public readonly int    dir_id = directory_id;
    public readonly string name   = file.Name;
    public readonly long   size   = file.Length;
    public readonly long   cdate  = file.CreationTimeUtc.ToFileTimeUtc();
    public readonly long   mdate  = file.LastWriteTimeUtc.ToFileTimeUtc();
}

public class DB_File_UpdateDate
    (int file_id, DateTime date)
{
    public readonly int    id     = file_id;
    public readonly long   date   = date.ToFileTimeUtc();
}

public class DB_File_UpdateDateSize
    (int file_id, DateTime date, Size size)
    : DB_File_UpdateDate(file_id, date)
{
    public readonly int image_w = size.Width;
    public readonly int image_h = size.Height;
}

public static class DB_Files
{
    // CREATE

    public static async Task Files_CreateMany
        (this SqliteConnection c, SqliteTransaction? transaction, IEnumerable<DB_File_Insert> files)
    {
        const string SQL =
            "INSERT OR IGNORE "
          + "INTO files (dir_id, name, size, cdate, mdate) "
          + "VALUES (@dir_id, @name, @size, @cdate, @mdate)";
        await c.ExecuteAsync(SQL, files, transaction);
    }

    // GET

    public static async Task<DB_File_Get_WithPath?> File_GetPath
        (this SqliteConnection c, int id)
    {
        const string SQL =
            "SELECT f.id, d.path, f.name "
          + "FROM files f "
          + "JOIN dirs d ON d.id = f.dir_id "
          + "WHERE f.id = @id";
        return await c.QuerySingleOrDefaultAsync<DB_File_Get_WithPath>(SQL, new { id });
    }

    public static async Task<IEnumerable<DB_File_Get_WithPath>> Files_GetToBeAnalyzed
        (this SqliteConnection c)
    {
        const string SQL =
            "SELECT f.id, d.path, f.name "
          + "FROM files f "
          + "JOIN dirs d ON d.id = f.dir_id "
          + "WHERE adate IS NULL OR mdate > adate";
        return await c.QueryAsync<DB_File_Get_WithPath>(SQL);
    }

    public static async Task<IEnumerable<DB_File_Get_WithPath>> Files_GetToBeThumbed
        (this SqliteConnection c)
    {
        const string SQL =
            "SELECT f.id, d.path, f.name "
          + "FROM files f "
          + "JOIN dirs d ON d.id = f.dir_id "
          + "WHERE tdate IS NULL OR mdate > tdate";
        return await c.QueryAsync<DB_File_Get_WithPath>(SQL);
    }

    public static async Task<IEnumerable<DB_File_Get_ForSync>> Files_ForSync_GetByDirIds
        (this SqliteConnection c, IEnumerable<int> dir_ids)
    {
        var SQL =
            "SELECT id, dir_id, name, size, cdate, mdate "
            + "FROM files f "
            + $"WHERE dir_id IN ({string.Join(',', dir_ids)})";
        return await c.QueryAsync<DB_File_Get_ForSync>(SQL);
    }

    public static async Task<IEnumerable<DB_File_Get_UI>> Files_UI_GetBySQL_Simple
        (this SqliteConnection c, string SQL)
    {
        return await c.QueryAsync<DB_File_Get_UI>(SQL);
    }

    public static async Task<IEnumerable<DB_File_Get_UI_WithCount>> Files_UI_GetBySQL_WithCount
        (this SqliteConnection c, string SQL)
    {
        return await c.QueryAsync<DB_File_Get_UI_WithCount>(SQL);
    }

    // UPDATE

    public static async Task File_Update
        (this SqliteConnection c, SqliteTransaction? transaction, DB_File_Update file)
    {
        const string SQL =
            "UPDATE files "
            + "SET dir_id = @dir_id, name = @name, size = @size, cdate = @cdate, mdate = @mdate "
            + "WHERE id = @id";
        await c.ExecuteAsync(SQL, file, transaction);
    }

    public static async Task File_UpdateDateAnalyzed
        (this SqliteConnection c, DB_File_UpdateDate file)
    {
        const string SQL = "UPDATE files SET adate = @date WHERE id = @id";
        await c.ExecuteAsync(SQL, file);
    }

    public static async Task File_UpdateDateThumbGenerated
        (this SqliteConnection c, DB_File_UpdateDateSize file)
    {
        const string SQL =
            "UPDATE files "
          + "SET tdate = @date, image_w = @image_w, image_h = @image_h "
          + "WHERE id = @id";
        await c.ExecuteAsync(SQL, file);
    }

    // DELETE

    public static async Task File_Delete
        (this SqliteConnection c, SqliteTransaction? transaction, int id)
    {
        const string SQL = "DELETE FROM files WHERE id = @id";
        await c.ExecuteAsync(SQL, new { id }, transaction);
    }
}
using Dapper;
using Microsoft.Data.Sqlite;
using SixLabors.ImageSharp;

namespace MemeIndex.DB;

public class DB_File
{
    public int       Id;
    public int       DirectoryId;
    public string    Name;
    public string    Directory;
    public long      FileSize;
    public DateTime  DateCreated;
    public DateTime  DateModified;
    public DateTime? DateAnalyzed;
    public DateTime? DateThumbed;
    public Size?     ImageSize;
}

public class DB_File_WithPath
{
    public required int    id;
    public required string path;
    public required string name;

    public string GetPath() => Path.Combine(path, name);
}

public class DB_File_Insert(FileInfo info, int directory_id)
{
    public readonly int    dir_id = directory_id;
    public readonly string name   = info.Name;
    public readonly long   size   = info.Length;
    public readonly long   cdate  = info. CreationTimeUtc.ToFileTimeUtc();
    public readonly long   mdate  = info.LastWriteTimeUtc.ToFileTimeUtc();
}

public class DB_File_UpdateDate(int file_id, DateTime date)
{
    public readonly int    id     = file_id;
    public readonly long   date   = date.ToFileTimeUtc();
}

public static class DB_Files
{
    public static async Task Files_CreateMany
        (this SqliteConnection c, IEnumerable<DB_File_Insert> files)
    {
        const string SQL =
            "INSERT OR IGNORE "
          + "INTO files (dir_id, name, size, cdate, mdate) "
          + "VALUES (@dir_id, @name, @size, @cdate, @mdate)";
        await c.ExecuteAsync(SQL, files);
    }

    public static async Task<IEnumerable<DB_File_WithPath>> Files_GetToBeAnalyzed
        (this SqliteConnection c)
    {
        const string SQL =
            "SELECT f.id, d.path, f.name "
          + "FROM files f "
          + "JOIN dirs d ON d.id = f.dir_id "
          + "WHERE adate IS NULL OR mdate > adate";
        return await c.QueryAsync<DB_File_WithPath>(SQL);
    }

    public static async Task File_UpdateDateAnalyzed
        (this SqliteConnection c, DB_File_UpdateDate file)
    {
        const string SQL = "UPDATE files SET adate = @date WHERE id = @id";
        await c.ExecuteAsync(SQL, file);
    }
}
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

public class DB_File_Insert(FileInfo info, int directoryId)
{
    public readonly int    dir_id = directoryId;
    public readonly string name   = info.Name;
    public readonly long   size   = info.Length;
    public readonly long   cdate  = info. CreationTimeUtc.ToFileTimeUtc();
    public readonly long   mdate  = info.LastWriteTimeUtc.ToFileTimeUtc();
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
}
using System.Threading.Channels;
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class Command_AddFilesToDB
{
    public static readonly Channel<int>
        C_FileProcessing = Channel.CreateUnbounded<int>();

    private static readonly string[] _supported_extensions
        = [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"];

    public static async Task Execute(string directory, bool recursive)
    {
        var sw = Stopwatch.StartNew();

        // GET FILES INFO
        var di = new DirectoryInfo(directory);
        var option = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        var files = di.GetFiles("*.*", option)
            .Where(x => x.DirectoryName != null 
                     && _supported_extensions.Contains(x.Extension))
            .ToArray();
        var dir_paths = files
            .Select(x => x.DirectoryName!)
            .Prepend(di.FullName)
            .Distinct();
        sw.Log("[AddFilesToDB] GET FILES INFO");

        // ADD DIRS
        await using var con = await AppDB.ConnectTo_Main();
        await con.Dirs_CreateMany(dir_paths);
        var directories = await con.Dirs_GetAll();
        var directory_ids = directories.ToDictionary(x => x.Path, x => x.Id);

        // ADD FILES
        var file_insert = files
            .Select(x => new DB_File_Insert(x, directory_ids[x.DirectoryName!]));
        await con.Files_CreateMany(file_insert);
        await con.CloseAsync();
        sw.Log("[AddFilesToDB] ADD DIRS & FILES");

        await C_FileProcessing.Writer.WriteAsync(1);
    }
}
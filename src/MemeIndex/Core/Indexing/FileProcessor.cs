using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class FileProcessor
{
    // files.txt -> paths -> add files -> get files to process -> process -> add tags.

    private static readonly string[] _supported_extensions
        = [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"];

    public static async Task AddFilesToDB(string directory, bool recursive)
    {
        var sw = Stopwatch.StartNew();
        var di = new DirectoryInfo(directory);
        var option = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        var filesAll = di.GetFiles("*.*", option);

        var filesToAdd = filesAll
            .Where(x => x.DirectoryName != null 
                     && _supported_extensions.Contains(x.Extension))
            .ToArray();
        var dirPaths = filesToAdd
            .Select(x => x.DirectoryName!)
            .Prepend(di.FullName)
            .Distinct();
        sw.Log("[IX] GET FILES INFO");

        // ADD DIRS
        await using var con = await DB.DB.ConnectTo_Main();
        sw.Log("[DB] CONNECT");
        await con.Dirs_CreateMany(dirPaths);
        sw.Log("[DB] DIRS INSERT");
        var directories = await con.Dirs_GetAll();
        sw.Log("[DB] DIRS GET");
        var directoryIds = directories.ToDictionary(x => x.Path, x => x.Id);

        // ADD FILES
        var fileToInsert = filesToAdd
            .Select(x => new DB_File_Insert(x, directoryIds[x.DirectoryName!]));
        sw.Log("...");
        await con.Files_CreateMany(fileToInsert);
        sw.Log("[DB] FILES INSERT");
        await con.CloseAsync();
        sw.Log("[DB] CLOSE");

        // trigger new files processing
    }

    public static void TriggerAnalysis()
    {
        // ask db for files to process (no adate | mdate > adate)
        // pass paths to processor
    }

    public static void AnalyzeFiles(IEnumerable<string> files)
    {
        // analyze each file
        // send tags to db tag writer
        // failed to analyze -> write file id to broken files
    }

    public static void AddTagsToDB()
    {
        // write tags to db in batches
    }
}
using MemeIndex.Core.Analysis.Color.v2;
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
        await TriggerAnalysis();
    }

    public static async Task TriggerAnalysis()
    {
        var sw = Stopwatch.StartNew();
        await using var con = await DB.DB.ConnectTo_Main();
        sw.Log("[DB] CONNECT");
        var files = await con.Files_GetToBeAnalyzed();
        sw.Log("[DB] FILES GET TBA");
        await con.CloseAsync();
        sw.Log("[DB] CLOSE");

        // pass paths to processor
        await AnalyzeFiles(files);
    }

    public static async Task AnalyzeFiles(IEnumerable<DB_File_WithPath> files)
    {
        Log("AnalyzeFiles...");
        foreach (var file in files)
        {
            try
            {
                var tags = await ColorTagger_v2.AnalyzeImage(file.GetPath());
                var date = DateTime.UtcNow;

                // send tags to db tag writer
                await AddTagsToDB(tags, file.id, date);
            }
            catch (Exception e)
            {
                LogError(e);
                // add file id to broken files
            }
        }
        Log("AnalyzeFiles!");
    }

    public static async Task AddTagsToDB(IEnumerable<TagContent> tags, int file_id, DateTime date)
    {
        var sw = Stopwatch.StartNew();
        await using var con = await DB.DB.ConnectTo_Main();
        sw.Log("[DB] CONNECT");
        await con.Tags_CreateMany(tags.Select(x => new BD_Tag_Insert(x, file_id)));
        sw.Log("[DB] TAGS ADD");
        await con.File_UpdateDateAnalyzed(new DB_File_UpdateDate(file_id, date));
        sw.Log("[DB] FILE UPDATE DATE");
        await con.CloseAsync();
        sw.Log("[DB] CLOSE");

        // write tags to db in batches
    }
}
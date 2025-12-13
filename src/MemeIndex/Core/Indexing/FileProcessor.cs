using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class FileProcessor
{
    // files.txt -> paths -> add files -> get files to process -> process -> add tags.

    public static Channel<int>
        C_Thumbgen = Channel.CreateUnbounded<int>(),
        C_Analysis = Channel.CreateUnbounded<int>();
    public static Channel<AnalysisResult>
        C_AnalysisSave = Channel.CreateUnbounded<AnalysisResult>();
    public static Channel<ThumbgenResult>
        C_ThumbgenSave = Channel.CreateUnbounded<ThumbgenResult>();

    private static readonly string[] _supported_extensions
        = [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"];

    public static async Task AddFilesToDB(string directory, bool recursive)
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

        await C_Analysis.Writer.WriteAsync(1);
        await C_Thumbgen.Writer.WriteAsync(1);
    }

    public static async Task AnalyzeFiles()
    {
        sw0.Restart();
        Log("AnalyzeFiles", "START");

        await using var con = await AppDB.ConnectTo_Main();
        var files = await con.Files_GetToBeAnalyzed();
        await con.CloseAsync();
        Log("AnalyzeFiles", "GET FILES TBA");

        foreach (var file in files)
        {
            try
            {
                var sw2 = Stopwatch.StartNew();
                var tags = await ColorTagger_v2.AnalyzeImage(file.GetPath());
                Log($"Analyzed file {file.id,5}");
                time_ca += sw2.GetElapsed_Restart();
                N_files++;
                var date = DateTime.UtcNow;

                // send tags to db tag writer
                var result = new AnalysisResult(file.id, date, tags);
                await C_AnalysisSave.Writer.WriteAsync(result);
            }
            catch (Exception e)
            {
                LogError(e);
                // add file id to broken files
            }
        }
        Log("AnalyzeFiles", "DONE");
        var time = sw0.Elapsed;

        await Task.Delay(1000);
        Log($"""
             ANALYSIS DONE:
                 Files: {N_files,3}
                 Tags:  {N_tags ,3}
                 Time:           {time      .ReadableTime()} | {(time       / N_files).ReadableTime()} per file
                 DB    connect:  {con_open  .ReadableTime()} | {(con_open   / N_files).ReadableTime()} per file
                 DB disconnect:  {con_close .ReadableTime()} | {(con_close  / N_files).ReadableTime()} per file
                 DB write tags:  {time_tags .ReadableTime()} | {(time_tags  / N_files).ReadableTime()} per file
                 DB write files: {time_files.ReadableTime()} | {(time_files / N_files).ReadableTime()} per file
                 Color analysis: {time_ca   .ReadableTime()} | {(time_ca    / N_files).ReadableTime()} per file
             """);
    }

    public static async Task AddTagsToDB(AnalysisResult result)
    {
        var sw = Stopwatch.StartNew();
        await using var con = await AppDB.ConnectTo_Main();
        con_open += sw.GetElapsed_Restart();
        var rows = await con.Tags_CreateMany(result.tags.Select(x => new BD_Tag_Insert(x, result.file_id)));
        time_tags += sw.GetElapsed_Restart();
        await con.File_UpdateDateAnalyzed(new DB_File_UpdateDate(result.file_id, result.date));
        time_files += sw.GetElapsed_Restart();
        await con.CloseAsync();
        con_close += sw.GetElapsed_Restart();
        N_tags += rows;
    }

    // STATS
    private static int
        N_files = 0,
        N_tags  = 0;
    private static readonly Stopwatch sw0 = new();
    private static TimeSpan
        con_open   = TimeSpan.Zero,
        con_close  = TimeSpan.Zero,
        time_tags  = TimeSpan.Zero,
        time_files = TimeSpan.Zero,
        time_ca    = TimeSpan.Zero;
}

public record AnalysisResult(int file_id, DateTime date, IEnumerable<TagContent> tags);
public record ThumbgenResult(int file_id, DateTime date);
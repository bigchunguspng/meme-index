using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using Size = SixLabors.ImageSharp.Size;

namespace MemeIndex.Core.Indexing;

public static class FileProcessor
{
    // todo make channels temporary - created once for task: add files to db -> trigger file processing
    public static Channel<int>
        C_Thumbgen = Channel.CreateUnbounded<int>(),
        C_Analysis = Channel.CreateUnbounded<int>();
    public static Channel<AnalysisResult>
        C_AnalysisSave = Channel.CreateUnbounded<AnalysisResult>();
    public static Channel<ThumbgenResult>
        C_ThumbgenSave = Channel.CreateUnbounded<ThumbgenResult>();

    public static ImagePool ImagePool = new();

    private static readonly string[] _supported_extensions
        = [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"];

    // ADD FILES TO DB

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

    // ANALYZE

    public static async Task AnalyzeFiles()
    {
        N_files = N_tags = 0;
        sw0.Restart();
        Log("AnalyzeFiles", "START");

        await using var con = await AppDB.ConnectTo_Main();
        var files = await con.Files_GetToBeAnalyzed();
        await con.CloseAsync();
        Log("AnalyzeFiles", "GET FILES TBA");

        var filesIP = files
            .Select(x => new { Id = x.id, Path = x.GetPath() })
            .ToArray();
        ImagePool.Book(filesIP.Select(x => x.Path));
        foreach (var file in filesIP)
        {
            try
            {
                var sw2 = Stopwatch.StartNew();
                var tags = await AnalyzeImage(file.Path);
                Log($"Analyze file {file.Id,5}");
                time_ca += sw2.GetElapsed_Restart();
                N_files++;
                var date = DateTime.UtcNow;

                var result = new AnalysisResult(file.Id, date, tags);
                await C_AnalysisSave.Writer.WriteAsync(result);
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }
        Log("AnalyzeFiles", "DONE");
    }

    public static async Task<IEnumerable<TagContent>> AnalyzeImage
        (string path, int minScore = 10)
    {
        var sw = Stopwatch.StartNew();
        var image = await ImagePool.Load(path);
        time_cal += sw.GetElapsed_Restart();
        var report = ColorAnalyzer_v2.ScanImage(image);
        time_cas += sw.GetElapsed_Restart();
        var tags = ColorTagger_v2.AnalyzeImageScan(report, minScore);
        time_caa += sw.GetElapsed_Restart();
        return tags
            .Select(x => new TagContent(x.Key, x.Value));
    }

    // DB UPDATE

    public static async Task AddTagsToDB(AnalysisResult result)
    {
        var sw = Stopwatch.StartNew();
        await using var con = await AppDB.ConnectTo_Main();
        con_open += sw.GetElapsed_Restart();
        var rows = await con.Tags_CreateMany(result.ToDB_Tags());
        time_tags += sw.GetElapsed_Restart();
        await con.File_UpdateDateAnalyzed(result.ToDB_File());
        time_files += sw.GetElapsed_Restart();
        await con.CloseAsync();
        con_close += sw.GetElapsed_Restart();
        N_tags += rows;
        _time = sw0.Elapsed;
    }

    public static async Task UpdateFileThumbDateInDB(ThumbgenResult result)
    {
        var sw = Stopwatch.StartNew();
        await using var con = await AppDB.ConnectTo_Main(); // todo try to reuse connection / batch queries
        con_open += sw.GetElapsed_Restart();
        await con.File_UpdateDateThumbGenerated(result.ToDB_File());
        time_thumb += sw.GetElapsed_Restart();
        await con.CloseAsync();
        con_close += sw.GetElapsed_Restart();
        _time = sw0.Elapsed;
    }

    // STATS

    public static void PrintStats()
    {
        Log($"""
             ANALYSIS DONE:
                 Files: {N_files,3}
                 Tags:  {N_tags ,3}
                 Time:           {_time     .ReadableTime(),10} | {(_time      / N_files).ReadableTime(),10} per file
                 DB    connect:  {con_open  .ReadableTime(),10} | {(con_open   / N_files).ReadableTime(),10} per file
                 DB disconnect:  {con_close .ReadableTime(),10} | {(con_close  / N_files).ReadableTime(),10} per file
                 DB write tags:  {time_tags .ReadableTime(),10} | {(time_tags  / N_files).ReadableTime(),10} per file
                 DB upd files A: {time_files.ReadableTime(),10} | {(time_files / N_files).ReadableTime(),10} per file
                 DB upd files T: {time_thumb.ReadableTime(),10} | {(time_thumb / N_files).ReadableTime(),10} per file
                 Color analysis: {time_ca   .ReadableTime(),10} | {(time_ca    / N_files).ReadableTime(),10} per file
                 Thumb load:     {time_tgl  .ReadableTime(),10} | {(time_tgl   / N_files).ReadableTime(),10} per file
                 Thumb resize:   {time_tgr  .ReadableTime(),10} | {(time_tgr   / N_files).ReadableTime(),10} per file
                 Thumb save:     {time_tgs  .ReadableTime(),10} | {(time_tgs   / N_files).ReadableTime(),10} per file
                 C/A load:       {time_cal  .ReadableTime(),10} | {(time_cal   / N_files).ReadableTime(),10} per file
                 C/A scan:       {time_cas  .ReadableTime(),10} | {(time_cas   / N_files).ReadableTime(),10} per file
                 C/A analyze:    {time_caa  .ReadableTime(),10} | {(time_caa   / N_files).ReadableTime(),10} per file
             """);
    }

    public static int
        N_files,
        N_tags;
    public static readonly Stopwatch sw0 = new();
    public static TimeSpan
        _time      = TimeSpan.Zero,
        con_open   = TimeSpan.Zero,
        con_close  = TimeSpan.Zero,
        time_tags  = TimeSpan.Zero,
        time_files = TimeSpan.Zero,
        time_thumb = TimeSpan.Zero,
        time_ca    = TimeSpan.Zero,
        time_tgl   = TimeSpan.Zero,
        time_tgr   = TimeSpan.Zero,
        time_tgs   = TimeSpan.Zero,
        time_cal   = TimeSpan.Zero,
        time_cas   = TimeSpan.Zero,
        time_caa   = TimeSpan.Zero;
}

public record AnalysisResult(int file_id, DateTime date, IEnumerable<TagContent> tags)
{
    public DB_File_UpdateDate ToDB_File
        () => new(file_id, date);

    public IEnumerable<DB_Tag_Insert> ToDB_Tags
        () => tags.Select(x => new DB_Tag_Insert(x, file_id));
}

public record ThumbgenResult(int file_id, DateTime date, Size size)
{
    public DB_File_UpdateDateSize ToDB_File
        () => new(file_id, date, size);
}
using System.Threading.Channels;
using MemeIndex.Core.Analysis.Color.v2;
using MemeIndex.DB;
using MemeIndex.Utils;
using Microsoft.Data.Sqlite;
using Size = SixLabors.ImageSharp.Size;

namespace MemeIndex.Core.Indexing;

public static class FileProcessor
{
    public static readonly Channel<int>
        C_FileProcessing = Channel.CreateUnbounded<int>();

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

        await C_FileProcessing.Writer.WriteAsync(1);
    }
}

public partial class FileProcessingTask
{
    private readonly Channel<Func<SqliteConnection, Task>>
        C_DB_Write = Channel.CreateUnbounded<Func<SqliteConnection, Task>>();

    private static readonly ImagePool ImagePool = new();

    public async Task Run()
    {
        // START JOBS
        var job_DB = new Job_DB_Write(C_DB_Write, Tracer);
        var jobs = new BackgroundService[]
        {
            job_DB,
            //new Job_ThumbgenResize(this),
            new Job_ThumbgenSaveWebp(this),
        };
        foreach (var job in jobs)
        {
            await job.StartAsync(CancellationToken.None);
        }

        // LAUNCH TASKS
        _ = GenerateThumbnails();
        _ = AnalyzeFiles();

        // WAIT FOR JOBS TO FINISH
        var jobTasks = jobs
            .Skip(1)
            .Select(x => x.ExecuteTask)
            .OfType<Task>();
        await Task.WhenAll(jobTasks);

        // WAIT FOR DB WRITER JOB TO FINISH
        C_DB_Write.Writer.Complete();
        if (null != job_DB.ExecuteTask)
            await   job_DB.ExecuteTask;

        SaveEventLog();
    }

    // ANALYZE

    private async Task AnalyzeFiles()
    {
        Log("AnalyzeFiles", "START");

        // GET FILES
        await using var con = await AppDB.ConnectTo_Main();
        var files = await con.Files_GetToBeAnalyzed();
        await con.CloseAsync();
        Log("AnalyzeFiles", "GET FILES");

        var filesIP = files
            .Select(x => new { Id = x.id, Path = x.GetPath() })
            .ToArray();
        ImagePool.Book(filesIP.Select(x => x.Path), filesIP.Length);

        foreach (var file in filesIP)
        {
            try
            {
                var tags = await AnalyzeImage(file.Id, file.Path);
                Log($"Analyze file {file.Id,5}");
                var date = DateTime.UtcNow;

                var result = new AnalysisResult(file.Id, date, tags);
                await C_DB_Write.Writer.WriteAsync(async connection =>
                {
                    await connection.Tags_CreateMany        (result.ToDB_Tags());
                    await connection.File_UpdateDateAnalyzed(result.ToDB_File());
                });
            }
            catch (Exception e)
            {
                LogError(e);
                // todo add file id to broken files
            }
        }
        Log("AnalyzeFiles", "DONE");
    }

    private async Task<IEnumerable<TagContent>> AnalyzeImage
        (int id, string path, int minScore = 10)
    {
        Tracer.LogStart(CA_LOAD, id);
        var image = await ImagePool.Load(path);
        Tracer.LogBoth (CA_LOAD, id, CA_SCAN);
        var report = ColorAnalyzer_v2.ScanImage(image);
        ImagePool.Return(path);
        Tracer.LogBoth (CA_SCAN, id, CA_ANAL);
        var tags = ColorTagger_v2.AnalyzeImageScan(report, minScore);
        Tracer.LogEnd  (CA_ANAL, id);
        return tags
            .Select(x => new TagContent(x.Key, x.Value));
    }

    // STATS

    private readonly TraceCollector Tracer = new();

    public const string // event logger lanes
        DB_WRITE = "DB Write",
        CA_LOAD = "Color Analysis / Load",
        CA_SCAN = "Color Analysis / Scan",
        CA_ANAL = "Color Analysis / Analyze",
        THUMB_LOAD = "Thumbnail / Load",
        THUMB_SIZE = "Thumbnail / Resize",
        THUMB_SAVE = "Thumbnail / Save";

    private void SaveEventLog()
    {
        var save = Dir_Traces
            .EnsureDirectoryExist()
            .Combine($"File-processing-{Desert.Clock(24):x}.json");
        Tracer.SaveAs(save, AppJsonSerializerContext.Default.DictionaryStringListTrace);
        Tracer.PrintStats();
        Log($"SaveEventLog - \"{save}\"!");
    }
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
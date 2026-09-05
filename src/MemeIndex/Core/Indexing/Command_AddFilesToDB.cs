using System.Threading.Channels;
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class Indexing
{
    public static readonly HashSet<string> SupportedExtensions
        = [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"];
}

public static class Command_AddFilesToDB
{
    public static readonly Channel<int>
        C_FileProcessing = Channel.CreateUnbounded<int>();

    /// this will be called when user
    /// adds new tracking rules / directories
    /// (once for each OR [rewrite code] for many)   ← remove when this happens
    /// <br/>
    /// Adds directory and supported files from it to DB.
    /// Triggers file processing job if it's not active.
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
                     && Indexing.SupportedExtensions.Contains(x.Extension))
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
        var directory_ids = directories.ToDictionary(x => x.path, x => x.id);

        // ADD FILES
        var file_insert = files
            .Select(x => new DB_File_Insert(x, directory_ids[x.DirectoryName!]));
        await con.Files_CreateMany(file_insert);
        await con.CloseAsync();
        sw.Log("[AddFilesToDB] ADD DIRS & FILES");

        await C_FileProcessing.Writer.WriteAsync(1);
        await EnsureStarted_Job_FileProcessing();
    }

    private static Job_FileProcessing? Job;

    private static async Task EnsureStarted_Job_FileProcessing()
    {
        var new_job = TryReloadJob();
        if (new_job != null)
            await new_job.StartAsync(CancellationToken.None);
    }

    [MethodImpl(Synchronized)]
    private static Job_FileProcessing? TryReloadJob()
        => Job == null
        || Job.ExecuteTask is { IsCompleted: true }
            ? Job = new Job_FileProcessing()
            : null;

    private class Job_FileProcessing()
        : ChannelJob_ExecuteOrStop
        (
            "Job/FileProcessing",
            C_FileProcessing,
            async () => await new FileProcessor().Run(),
            "Task done!"
        );
}
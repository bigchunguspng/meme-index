using System.Threading.Channels;
using MemeIndex.Core.Search;
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public static class Indexing
{
    public static readonly HashSet<string>
        SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".webp"
        };

    // SYNC

    /// Synchronizes files and directories in DB with actual FS.
    /// Should be called on monitors update / manual sync / startup.
    /// Triggers file processing job if it's not active.
    /// <param name="syncAllMonitors"> Use <c>false</c> to sync only active ones. </param>
    public static async Task Sync(bool syncAllMonitors = false)
    {
        Log("[Sync]", syncAllMonitors ? "ALL" : "ACTIVE ONLY");
        var sw = Stopwatch.StartNew();

        // GET DB MONITORS
        await using var con = await AppDB.ConnectTo_Main();
        var db_monitors = await con.Monitors_GetAll();
        var    monitors_toProcess = db_monitors
            .Where(x => syncAllMonitors || x.enabled)
            .ToList();
        sw.Log($"[Sync] DB GET MONITORS: {monitors_toProcess.Count} to process");

        // GET DB DIRS
        var db_dirs = await con.Dirs_GetAll();
        var    dirs = db_dirs.ToList();
        var    dirIds_ByPath = dirs.ToDictionary(x => x.path, x => x.id);
        var    dirPaths_ById = dirs.ToDictionary(x => x.id, x => x.path);
        sw.Log($"[Sync] DB GET DIRS: {dirs.Count}");

        // GET FS FILES, ADD MISSING DB DIRS, MATCH TO DB (per monitor)
        var sw_m = Stopwatch.StartNew();
        var mismatch = new DB_And_FS_Mismatch();

        foreach (var monitor in monitors_toProcess)
        {
            // GET FS FILES
            var fs_files = Monitor_GetFiles(monitor).ToList();
            sw_m.Log($"[Sync|Monitor-{monitor.id:00}] FS GET FILES: {fs_files.Count}");

            // UPDATE DB DIRS
            {
                var set_new_dir_paths = fs_files
                    .Select(x => x.DirectoryName!)
                    .ToHashSet()
                    .Except(dirIds_ByPath.Keys);

                await con.Dirs_CreateMany(set_new_dir_paths);
                var db_dirs_updated = await con.Dirs_GetAll();
                var    dirs_updated = db_dirs_updated.ToList();

                var new_dirs_count = dirs_updated.Count - dirs.Count;
                var new_dir_ids = dirs_updated
                    .Select(x => x.id)
                    .ToHashSet()
                    .Except(dirPaths_ById.Keys);
                var new_dirs = dirs_updated.Where(x => new_dir_ids.Contains(x.id));
                foreach (var dir in new_dirs)
                {
                    dirs.Add(dir);
                    dirIds_ByPath[dir.path] = dir.id;
                    dirPaths_ById[dir.id] = dir.path;
                }
                sw_m.Log($"[Sync|Monitor-{monitor.id:00}] DB ADD DIRS: {new_dirs_count}");
            }

            // GET DB FILES, MATCH FILES
            var db_monitor_dir_ids = monitor.recurse
                ? dirs
                    .Where(x => x.path.StartsWith(monitor.path))
                    .Select(x => x.id)
                    .ToList()
                : [monitor.dir_id];
            var db_files = await con.Files_ForSync_GetByDirIds(db_monitor_dir_ids);
            sw_m.Log($"[Sync|Monitor-{monitor.id:00}] DB GET FILES");

            mismatch.MatchFiles_DB_and_FS(db_files, fs_files, dirPaths_ById);
            sw_m.Log($"[Sync|Monitor-{monitor.id:00}] MATCH FILES");
        }
        var unk = mismatch.fs_unknown.Count;
        var mis = mismatch.db_missing.Count;
        var chg = mismatch.db_changed.Count;
        sw.Log($"[Sync] PROCESS MONITORS, MISMATCH: {unk}/{mis}/{chg} (unk/mis/chg)");

        // MATCH FILES (globally), PREPARE DB WRITES
        List<DB_File_Insert> w_new = [];
        List<DB_File_Update> w_upd = [];
        List<int>            w_del = [];

        // try to match unknown to missing by size & dates:
        // -    match -> moved (new path)(upd, remove from missing)
        // - no match -> new file (new)
        // unknown -> new | upd
        // missing -> del
        // changed -> upd
        
        var db_missing_bySize = mismatch.db_missing
            .GroupBy(x => x.size)
            .ToDictionary(x => x.Key, x => x.ToList());
        // files are usually ADDED -> count of db files is usually SMALLER -> use db files for dic

        foreach (var unknown_fs_file in mismatch.fs_unknown)
        {
            var fs_size = unknown_fs_file.Length;
            if (db_missing_bySize.TryGetValue(fs_size, out var db_files))
            {
                foreach (var db_file in db_files) // db files with same size
                {
                    // try to find match
                    // same size, cdate & mdate = 99.999999…% same file content
                    if (db_file.cdate == unknown_fs_file. CreationTimeUtc.ToFileTimeUtc()
                     && db_file.mdate == unknown_fs_file.LastWriteTimeUtc.ToFileTimeUtc())
                    {
                        // Ladies & Gentlemen… WE GOT HIM!
                        mismatch.db_changed.Add((db_file, unknown_fs_file));

                        // remove it from missing!
                        mismatch.db_missing.Remove(db_file);
                        var db_files_withSameSize = db_missing_bySize[db_file.size];
                        if (db_files_withSameSize.Count > 1)
                            db_files_withSameSize.Remove(db_file);
                        else
                            db_missing_bySize.Remove(db_file.size);
                    }
                }
            }
            else // no db file with same size -> NEW
            {
                var dir_id =   dirIds_ByPath[unknown_fs_file.DirectoryName!];
                w_new.Add(new DB_File_Insert(unknown_fs_file, dir_id));
            }
        }

        foreach (var (og_db_file, changed_fs_file) in mismatch.db_changed)
        {
            // changed cases:
            // 1. same path & name, diff size | dates (edited)
            // 2. diff path | name, same size & dates (moved)
            // all same - not changed, all diff - new
            var dir_id = dirIds_ByPath[changed_fs_file.DirectoryName!];
            w_upd.Add(new DB_File_Update(og_db_file.id, dir_id, changed_fs_file));
        }

        foreach (var missing_db_file in mismatch.db_missing)
        {
            w_del.Add(missing_db_file.id);
        }
        sw.Log("[Sync] MATCH FILES");

        // UPDATE DB FILES
        await using var transaction = con.BeginTransaction();
        if      (w_new.Count > 0)   await con.Files_CreateMany(transaction, w_new);
        foreach (var file in w_upd) await con.File_Update     (transaction, file);
        foreach (var file in w_del) await con.File_Delete     (transaction, file);
        await transaction.CommitAsync();
        await con.CloseAsync();
        Jarvis.Cache_Clear();
        sw.Log($"[Sync] DB UPDATE FILES: {w_new.Count}/{w_upd.Count}/{w_del.Count} (new/upd/del)");

        // TRIGGER PROCESSING
        await C_FileProcessing.Writer.WriteAsync(1);
        await EnsureStarted_Job_FileProcessing();
        sw.Log("[Sync] TRIGGER PROCESSING");
    }

    private static IEnumerable<FileInfo> Monitor_GetFiles
        (DB_Monitor_Get monitor)
    {
        var di = new DirectoryInfo(monitor.path);
        var option = monitor.recurse
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;
        var files = di.GetFiles("*.*", option)
            .Where(x => x.DirectoryName != null
                     && SupportedExtensions.Contains(x.Extension));

        return files;
    }

    // MATCHING FILES

    private static void MatchFiles_DB_and_FS
    (
        this DB_And_FS_Mismatch mismatch,
        IEnumerable<DB_File_Get_ForSync> db_files,
        IEnumerable<FileInfo>            fs_files,
        Dictionary<int, string> dirPaths_ById
    )
    {
        var dic_db_files = db_files.ToDictionary(x => x.GetKey(dirPaths_ById), x => x);
        var dic_fs_files = fs_files.ToDictionary(x => x.GetKey(),              x => x);
        var keys_unknown = dic_fs_files.Keys.Except   (dic_db_files.Keys);
        var keys_missing = dic_db_files.Keys.Except   (dic_fs_files.Keys);
        var keys_other   = dic_db_files.Keys.Intersect(dic_fs_files.Keys);

        foreach (var k in keys_unknown) mismatch.fs_unknown.Add(dic_fs_files[k]);
        foreach (var k in keys_missing) mismatch.db_missing.Add(dic_db_files[k]);
        foreach (var k in keys_other)
        {
            var db = dic_db_files[k];
            var fs = dic_fs_files[k];

            if (db.size  != fs.Length
             || db.mdate != fs.LastWriteTimeUtc.ToFileTimeUtc())
            {
                mismatch.db_changed.Add((db, fs));
            }
        }
    }

    private static DB_File_Key GetKey
        (this DB_File_Get_ForSync file, Dictionary<int, string> dirPaths_ById)
        => new(dirPaths_ById[file.id], file.name);

    private static DB_File_Key GetKey
        (this FileInfo file)
        => new(file.DirectoryName!,    file.Name);

    private record struct DB_File_Key(string Directory, string Name);
    private        struct DB_And_FS_Mismatch()
    {
        public readonly List                       <FileInfo> fs_unknown = []; // fs+, db-
        public readonly List <DB_File_Get_ForSync>            db_missing = []; // db+, fs-
        public readonly List<(DB_File_Get_ForSync, FileInfo)> db_changed = []; // db+, fs was modified
    }

    // ADD SINGLE DIRECTORY (old test code)

    /// Adds directory and supported files from it to DB.
    /// Triggers file processing job if it's not active.
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
                     && SupportedExtensions.Contains(x.Extension))
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
        var files_insert = files
            .Select(x => new DB_File_Insert(x, directory_ids[x.DirectoryName!]));
        await using var transaction = con.BeginTransaction();
        await con.Files_CreateMany(transaction, files_insert);
        await transaction.CommitAsync();
        await con.CloseAsync();
        sw.Log("[AddFilesToDB] ADD DIRS & FILES");

        // TRIGGER PROCESSING
        await C_FileProcessing.Writer.WriteAsync(1);
        await EnsureStarted_Job_FileProcessing();
        sw.Log("[AddFilesToDB] TRIGGER PROCESSING");
    }

    // FILE PROCESSING JOB

    private static readonly Channel<int>
        C_FileProcessing  = Channel.CreateUnbounded<int>();

    private static Job_FileProcessing? Job;

    private static async Task EnsureStarted_Job_FileProcessing()
    {
        var new_job = TryReloadJob();
        if (new_job != null)
            await new_job.StartAsync(CancellationToken.None);
    }

    /// Creates (sets to <see cref="Job"/>) and returns a new job
    /// if it doesn't exist or was completed.
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
            "Task done!",
            App.LogException_JOB
        );
}
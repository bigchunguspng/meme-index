using System.Threading.Channels;
using MemeIndex.API;
using MemeIndex.DB;

namespace MemeIndex.Core.Indexing;

public record MonitorKey(string Path, int Method);
public class  MonitorValue
{
    public readonly int? Id;
    public readonly bool Recurse; // Apply to nested directories
    public readonly bool Enabled; // Monitor file changes, process directory during manual/startup syncs

    public MonitorValue(DB_Monitor_Get m)
    {
        Id = m.id;
        Recurse = m.recurse;
        Enabled = m.enabled;
    }

    public MonitorValue(API_Monitor_Post m)
    {
        Recurse = m.R;
        Enabled = m.E;
    }
}

public static class MonitorsDispatcher
{
    public static async Task<API_Monitors_Post_Response> UpdateMonitors(API_Monitors_Post_Request body)
    {
        Log("[Update Monitors]", $"COUNT: {body.M.Count}");
        var sw = Stopwatch.StartNew();

        // GET DIRS & MONITORS
        await using var con = await AppDB.ConnectTo_Main();
        var db_monitors = await con.Monitors_GetAll();
        var db_dirs_all = await con.Dirs_GetAll();
        var    dir_ids_byPath = db_dirs_all.ToDictionary(x => x.path, x => x.id);
        sw.Log("[Update Monitors] DB READ");

        // NOTE: db monitors are distinct by path and method!
        // UI:
        // PATH     | Color     | OCR
        // ~/memes  | Y *flags* | Y *flags*
        // ~/pics   | Y *flags* | N *flags*
        // DB:
        // 1 ~/memes Color  *flags*
        // 2 ~/memes OCR    *flags*
        // 3 ~/pics  Color  *flags*

        // DIFF MONITORS
        var dic_db_monitors = db_monitors
            .ToDictionary(
                x => new MonitorKey(x.path, x.method),
                x => new MonitorValue(x));
        var dic_nw_monitors = body.M // TL Note: nw = new
            .SelectMany(mbp => mbp.M.Select(m =>
            {
                var k = new MonitorKey(mbp.P, m.M);
                var v = new MonitorValue(m);
                return new KeyValuePair<MonitorKey, MonitorValue>(k, v);
            }))
            .ToDictionary();

        var set_db_monitors = dic_db_monitors.Keys.ToHashSet();
        var set_nw_monitors = dic_nw_monitors.Keys.ToHashSet();

        var keys_monitors_new  = set_nw_monitors.Except   (set_db_monitors);
        var keys_monitors_del  = set_db_monitors.Except   (set_nw_monitors);
        var keys_monitors_keep = set_db_monitors.Intersect(set_nw_monitors);

        // PREPARE DB WRITES, ADD MISSING DB DIRS
        List<DB_Monitor_Insert> monitors_new = [];
        List<DB_Monitor_Update> monitors_upd = [];
        List<int>               monitors_del = [];

        foreach (var key in keys_monitors_new)
        {
            var nw_m = dic_nw_monitors[key];
            if (dir_ids_byPath.TryGetValue_Failed(key.Path, out var dir_id))
            {
                // dir not in db ? add dir to db, update dic
                await con.Dir_Create(key.Path);
                var new_dir = await con.Dir_GetByPath(key.Path);
                dir_id = dir_ids_byPath[key.Path] = new_dir.id;
            }
            monitors_new.Add(new DB_Monitor_Insert(dir_id, key.Method, nw_m.Recurse, nw_m.Enabled));
        }

        foreach (var key in keys_monitors_keep)
        {
            var db_m = dic_db_monitors[key];
            var nw_m = dic_nw_monitors[key];
            if (db_m.Enabled != nw_m.Enabled 
             || db_m.Recurse != nw_m.Recurse)
            {
                monitors_upd.Add(new DB_Monitor_Update(db_m.Id!.Value, nw_m.Recurse, nw_m.Enabled));
            }
            // else (no changes) - ignore
        }

        foreach (var key in keys_monitors_del)
        {
            var db_m = dic_db_monitors[key];
            monitors_del.Add(db_m.Id!.Value);
        }

        sw.Log($"[Update Monitors] COMPARE: {dic_nw_monitors.Count} new vs {dic_db_monitors.Count} DB");

        // UPDATE DB MONITORS
        await using var transaction = con.BeginTransaction();
        if      (monitors_new.Count > 0)  await con.Monitors_CreateMany(transaction, monitors_new);
        foreach (var mon in monitors_upd) await con.Monitor_Update     (transaction, mon);
        foreach (var mon in monitors_del) await con.Monitor_Delete     (transaction, mon);
        await transaction.CommitAsync();
        await con.CloseAsync();
        var c_new = monitors_new.Count;
        var c_upd = monitors_upd.Count;
        var c_del = monitors_del.Count;
        sw.Log($"[Update Monitors] DB WRITE: {c_new}/{c_upd}/{c_del} (new/upd/del)");

        // TRIGGER INDEXING
        await C_Sync.Writer.WriteAsync(1);
        await EnsureStarted_Job_Sync();
        sw.Log("[Update Monitors] TRIGGER SYNC");

        // todo validate dirs exist b4 adding to db
        // todo validate no monitor is inside other recursive monitor

        return new API_Monitors_Post_Response
        {
            A = monitors_new.Count,
            U = monitors_upd.Count,
            D = monitors_del.Count,
        };
    }

    // SYNC JOB

    private static readonly Channel<int>
        C_Sync            = Channel.CreateUnbounded<int>();

    private static Job_C_Sync? Job;

    private static async Task EnsureStarted_Job_Sync()
    {
        var new_job = TryReloadJob();
        if (new_job != null)
            await new_job.StartAsync(CancellationToken.None);
    }

    /// Creates (sets to <see cref="Job"/>) and returns a new job
    /// if it doesn't exist or was completed.
    [MethodImpl(Synchronized)]
    private static Job_C_Sync? TryReloadJob()
        => Job == null
            || Job.ExecuteTask is { IsCompleted: true }
                ? Job = new Job_C_Sync()
                : null;

    private class Job_C_Sync()
        : ChannelJob_ExecuteOrStop
        (
            "Job/Sync",
            C_Sync,
            async () => await Indexing.Sync(),
            "Task done!",
            App.LogException_JOB
        );
}
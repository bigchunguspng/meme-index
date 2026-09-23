namespace MemeIndex.Tools.Logging;

public record TraceLane(int Id, string Title, string TaskIdMeaning)
{
    public List<TraceTask> Tasks { get; } = [];
}

public struct TraceTask(int id, int thread_id, DateTime start)
{
    public int      Id       { get; } = id;
    public int      ThreadId { get; } = thread_id;
    public DateTime Start    { get; } = start;
    public TimeSpan Duration { get; set; }
}

/// Use this to gather traces of array-processing tasks.
public class TraceCollector((string Title, string TaskMeaning)[] lanes)
{
    private readonly Dictionary<int, TraceLane> _lanes = lanes
        .Select((tuple, i) => new TraceLane(i + 1, tuple.Title, tuple.TaskMeaning))
        .ToDictionary(x => x.Id, x => x);

    public bool Empty           => _lanes.Count == 0;
    public int  Count(int lane) => _lanes[lane].Tasks.Count;

    /// Call this right before subtask is called.
    [MethodImpl(Synchronized)]
    public void LogOpen
        (int task, int lane) =>
        LogOpen(lane, task, DateTime.UtcNow);

    /// Call this right after subtask is done.
    [MethodImpl(Synchronized)]
    public void LogDone
        (int task, int lane) =>
        LogDone(lane, task, DateTime.UtcNow);

    /// Call this between 2 subtasks
    /// to log one's end, and another one's start.
    [MethodImpl(Synchronized)]
    public void LogJoin
        (int task, int lane_done, int lane_open)
    {
        var time = DateTime.UtcNow;
        LogDone(lane_done, task, time);
        LogOpen(lane_open, task, time);
    }

    //

    [MethodImpl(AggressiveInlining)]
    private void LogOpen
        (int lane_id, int task_id, DateTime time)
    {
        var tid = Environment.CurrentManagedThreadId;
        var task = new TraceTask(task_id, tid, time);
        _lanes[lane_id].Tasks.Add(task);
    }

    [MethodImpl(AggressiveInlining)]
    private void LogDone
        (int lane_id, int task_id, DateTime time)
    {
        var        tasks = _lanes[lane_id].Tasks;
        var    i = tasks.FindLastIndex(x => x.Id == task_id);
        var task = tasks[i];

        tasks[i] = task with { Duration = time - task.Start };
    }

    // EXPORT

    public async Task SaveAs(string path)
    {
        await using var writer = File.CreateText(path);
        await writer.WriteLineAsync("# Lane: id | task id meaning | title");
        await writer.WriteLineAsync("# Task: id | thread id | start | duration");
        foreach (var (_, l) in _lanes)
        {
            await writer.WriteLineAsync($"L:{l.Id}|{l.TaskIdMeaning}|{l.Title}");
            foreach (var t in l.Tasks)
            {
                await writer.WriteLineAsync($"T:{t.Id}|{t.ThreadId}|{t.Start:O}|{t.Duration:c}");
            }
        }
    }

    public void PrintStats()
    {
        var stats = _lanes.Select(kv =>
        {
            var (lane_id, lane) = kv;

            var tasks = lane.Tasks;
            // [t]imestamp | [d]uration, [t]icks | [s]econds.
            var t_min_t = tasks.Min    (x => x.Start.Ticks);
            var t_max_t = tasks.Max    (x => x.Start.Ticks);
            var d_len_s = (t_max_t - t_min_t).TicksToSeconds();
            var d_sum_s = tasks.Sum    (x => x.Duration.TotalSeconds);
            var d_pak_p = 100 * d_sum_s / d_len_s;
            var d_avg_s = tasks.Average(x => x.Duration.TotalSeconds);
            var d_min_s = tasks.Min    (x => x.Duration.TotalSeconds);
            var d_max_s = tasks.Max    (x => x.Duration.TotalSeconds);
            return new
            {
                lane_id, t_min_t, t_max_t,
                d_len_s, d_sum_s, d_pak_p,
                d_avg_s, d_min_s, d_max_s, tasks.Count,
            };
        }).OrderBy(x => x.lane_id).ToArray();

        long // global min/max
            T_min_t = stats.Min(x => x.t_min_t),
            T_max_t = stats.Max(x => x.t_max_t);

        var pad = _lanes.Values.Max(x => x.Title.Length);
        Print($"{"LANE".PadRight(pad)} "
            + $"| START     | SPAN      | SUM       | PACKING   "
            + $"| AVG       | MIN       | MAX       "
            + $"| N");
        foreach (var s in stats)
        {
            var lane = _lanes[s.lane_id];
            var d_off_s = (s.t_min_t - T_min_t).TicksToSeconds();
            var text = $"{lane.Title.PadRight(pad)}"
                  + $" | {  d_off_s,7:F3} s"
                  + $" | {s.d_len_s,7:F3} s"
                  + $" | {s.d_sum_s,7:F3} s"
                  + $" | {s.d_pak_p,6:F2}% s"
                  + $" | {s.d_avg_s,7:F3} s"
                  + $" | {s.d_min_s,7:F3} s"
                  + $" | {s.d_max_s,7:F3} s"
                  + $" | {s.Count,4}";
            Print(text);
        }
        var T_len_s = (T_max_t - T_min_t).TicksToSeconds();
        Print($"{"TIMELINE".PadRight(pad)} | {0,7:F3} s | {T_len_s,7:F3} s");
    }
}
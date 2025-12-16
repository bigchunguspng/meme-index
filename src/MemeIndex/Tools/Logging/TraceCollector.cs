using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MemeIndex.Tools.Logging;

/// Use this to gather traces of array processing tasks.
public class TraceCollector
{
    private readonly Dictionary<string, List<TraceSpan>> _log = [];

    [MethodImpl(Synchronized)]
    public void LogStart
        (string lane, int id) => 
        LogStart(lane, id, DateTime.UtcNow);

    /// Make sure trace start      (lane, id) was logged!
    [MethodImpl(Synchronized)]
    public void LogEnd
        (string lane, int id) => 
        LogEnd(lane, id, DateTime.UtcNow);

    /// Make sure trace start (prev_lane, id) was logged!
    [MethodImpl(Synchronized)]
    public void LogBoth
        (string lane_end, int id, string lane_start)
    {
        var time = DateTime.UtcNow;
        LogEnd  (lane_end,   id, time);
        LogStart(lane_start, id, time);
    }

    //

    [MethodImpl(AggressiveInlining)]
    private void LogStart
        (string lane, int id, DateTime time)
    {
        var tid = Thread.CurrentThread.ManagedThreadId;
        var item = new TraceSpan(id, tid, time);

        if (_log.TryGetValue(lane, out var traces))
            traces.Add(item);
        else
            _log[lane] = [item];
    }

    [MethodImpl(AggressiveInlining)]
    private void LogEnd
        (string lane, int id, DateTime time)
    {
        var item = _log[lane].FindLast(x => x.Id == id)!;

        item.Duration = time - item.TimeStart;
    }

    // EXPORT

    public void SaveAs
        (string path, JsonTypeInfo<Dictionary<string,List<TraceSpan>>> typeInfo)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(_log, typeInfo));
    }

    public void PrintStats()
    {
        var stats = _log.Select(kv =>
        {
            var (lane, traces) = kv;

            // [t]imestamp | [d]uration, [t]icks | [s]econds.
            var t_min_t = traces.Min    (x => x.TimeStart.Ticks);
            var t_max_t = traces.Max    (x => x.TimeStart.Ticks);
            var d_len_s = (t_max_t - t_min_t).TicksToSeconds();
            var d_sum_s = traces.Sum    (x => x.Duration.TotalSeconds);
            var d_avg_s = traces.Average(x => x.Duration.TotalSeconds);
            var d_min_s = traces.Min    (x => x.Duration.TotalSeconds);
            var d_max_s = traces.Max    (x => x.Duration.TotalSeconds);
            return new
            {
                lane,    t_min_t, t_max_t, d_len_s,
                d_sum_s, d_avg_s, d_min_s, d_max_s, traces.Count,
            };
        }).OrderBy(x => x.lane).ToArray();

        long // global min/max
            T_min_t = stats.Min(x => x.t_min_t),
            T_max_t = stats.Max(x => x.t_max_t);

        var pad = _log.Max(x => x.Key.Length);
        Print($"{"LANE".PadRight(pad)}"
            + $" | START     | SPAN     "
            + $" | SUM       | AVG      "
            + $" | MIN       | MAX      "
            + $" | N");
        foreach (var s in stats)
        {
            var d_off_s = (s.t_min_t - T_min_t).TicksToSeconds();
            var text = $"{s.lane.PadRight(pad)}"
                  + $" | {  d_off_s,7:F3} s"
                  + $" | {s.d_len_s,7:F3} s"
                  + $" | {s.d_sum_s,7:F3} s"
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

public class TraceSpan(int id, int thread_id, DateTime time)
{
    [JsonPropertyName("task_id"  )] public int      Id        { get; } = id;
    [JsonPropertyName("thread_id")] public int      ThreadId  { get; } = thread_id;
    [JsonPropertyName("start"    )] public DateTime TimeStart { get; } = time;
    [JsonPropertyName("duration" )] public TimeSpan Duration  { get; set; }
}
namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Time
{
    public static string ElapsedReadable
        (this Stopwatch sw) => sw.Elapsed.ReadableTime();

    public static string ReadableTime
        (this TimeSpan t)
        =>    t.TotalSeconds <  10 ? $@"{t:s\.fff}'{t.Microseconds/10:00} s"
            : t.TotalMinutes <   1 ? $@"{t:s\.fff' s'}"
            : t.TotalHours   <   1 ? $@"{t:m\:ss' m'}"
            : t.TotalDays    <   1 ? $@"{t:h\:mm' h'}"
            : t.TotalDays    < 100 ? $@"{t:d\.hh\:mm' d'}"
            :                         $"{t.TotalDays:F1} d";

    public static TimeSpan GetElapsed_Restart(this Stopwatch sw)
    {
        var elapsed = sw.Elapsed;
        sw.Restart();
        return elapsed;
    }

    public static double TicksToSeconds
        (this long ticks) => TimeSpan.FromTicks(ticks).TotalSeconds;

    public static bool HappenedWithinLast
        (this DateTime date, TimeSpan span) => DateTime.Now - date < span;
}
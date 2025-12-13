namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Time
{
    public static string ElapsedReadable
        (this Stopwatch sw) => sw.Elapsed.ReadableTime();

    public static string ReadableTime
        (this TimeSpan t)
        =>    t.TotalSeconds < 10 ? $@"{t:s\.fff}'{t.Microseconds/10:00} s"
            : t.TotalMinutes <  1 ? $@"{t:s\.fff' s'}"
            : t.TotalMinutes <  5 ? $"{t:m':'ss' M:SS'}"
            : t.TotalHours   <  1 ? $"{t:m' MINS'}"
            : t.TotalHours   <  5 ? $"{t:h':'mm' H:MM'}"
            : t.TotalDays    <  1 ? $"{t:h' HOURS'}"
            : t.TotalDays    <  2 ? $"{t:d' DAY'}"
            :                       $"{t:d' DAYS'}";

    public static TimeSpan GetElapsed_Restart(this Stopwatch sw)
    {
        var elapsed = sw.Elapsed;
        sw.Restart();
        return elapsed;
    }

    public static bool HappenedWithinLast
        (this DateTime date, TimeSpan span) => DateTime.Now - date < span;
}
namespace PgSafe.Utils;

public static class TimeFormatter
{
    public static string Humanize(TimeSpan time)
    {
        if (time.TotalMilliseconds < 1000)
            return $"{time.TotalMilliseconds:F0}ms";

        if (time.TotalSeconds < 60)
            return $"{time.TotalSeconds:F1}s";

        return $"{(int)time.TotalMinutes}m {time.Seconds}s";
    }
}
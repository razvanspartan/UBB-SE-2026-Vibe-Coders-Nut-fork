using System.Globalization;

namespace VibeCoders.Domain;

public static class ActiveTimeFormatter
{
    public static string ToHourMinuteSecond(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        var totalSeconds = (long)Math.Floor(duration.TotalSeconds);
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;
        return string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:D2}:{seconds:D2}");
    }

    public static double ToDecimalHours(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            return 0.0;
        }

        return duration.TotalHours;
    }

    public static double ToDecimalHours(int durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            return 0.0;
        }

        return durationSeconds / 3600.0;
    }
}

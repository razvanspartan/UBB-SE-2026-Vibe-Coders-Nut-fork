namespace VibeCoders.Domain;

using System.Globalization;

public static class ActiveTimeFormatter
{
    private const int SecondsInHour = 3600;
    private const int SecondsInMinute = 60;

    public static string ToHourMinuteSecond(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        var totalSeconds = (long)Math.Floor(duration.TotalSeconds);
        var hours = totalSeconds / SecondsInHour;
        var minutes = (totalSeconds % SecondsInHour) / SecondsInMinute;
        var seconds = totalSeconds % SecondsInMinute;
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

        return durationSeconds / (double)SecondsInHour;
    }
}

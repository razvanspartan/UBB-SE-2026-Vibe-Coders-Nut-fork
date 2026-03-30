namespace VibeCoders.Models.Analytics;

/// <summary>
/// One ISO-style week bucket for the 4-week consistency chart.
/// Weeks start on Monday (ISO 8601) and use the local calendar.
/// </summary>
public sealed class ConsistencyWeekBucket
{
    /// <summary>Monday (inclusive) of the ISO week.</summary>
    public DateOnly WeekStart { get; init; }

    /// <summary>Number of completed workouts logged within this week.</summary>
    public int WorkoutCount { get; init; }
}

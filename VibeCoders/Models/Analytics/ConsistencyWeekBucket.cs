namespace VibeCoders.Models.Analytics;

public sealed class ConsistencyWeekBucket
{
    public DateOnly WeekStart { get; init; }

    public int WorkoutCount { get; init; }
}

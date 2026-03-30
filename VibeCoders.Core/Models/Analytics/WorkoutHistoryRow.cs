namespace VibeCoders.Models.Analytics;

/// <summary>
/// Lightweight row for paginated workout history (unexpanded view).
/// </summary>
public sealed class WorkoutHistoryRow
{
    public int Id { get; init; }
    public string WorkoutName { get; init; } = string.Empty;

    /// <summary>Calendar date of the session (treat as local date, no timezone).</summary>
    public DateTime LogDate { get; init; }

    public int DurationSeconds { get; init; }
    public int TotalCaloriesBurned { get; init; }
    public string IntensityTag { get; init; } = string.Empty;
}

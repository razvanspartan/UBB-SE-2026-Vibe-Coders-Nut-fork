namespace VibeCoders.Models.Analytics;

public sealed class WorkoutHistoryRow
{
    public int Id { get; init; }
    public string WorkoutName { get; init; } = string.Empty;

    public DateTime LogDate { get; init; }

    public int DurationSeconds { get; init; }
    public int TotalCaloriesBurned { get; init; }
    public string IntensityTag { get; init; } = string.Empty;
}

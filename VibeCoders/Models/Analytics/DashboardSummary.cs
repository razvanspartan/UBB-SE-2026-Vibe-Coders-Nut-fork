namespace VibeCoders.Models.Analytics;

public sealed class DashboardSummary
{
    public int TotalWorkouts { get; init; }

    public TimeSpan TotalActiveTimeLastSevenDays { get; init; }

    public string? PreferredWorkoutName { get; init; }
}

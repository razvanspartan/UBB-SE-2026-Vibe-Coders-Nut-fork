namespace VibeCoders.Models.Analytics;

/// <summary>
/// Aggregated KPIs for the client analytics dashboard.
/// Covers total workouts, trailing-7-day active time, and preferred workout.
/// </summary>
public sealed class DashboardSummary
{
    /// <summary>Total number of completed workouts for the user.</summary>
    public int TotalWorkouts { get; init; }

    /// <summary>
    /// Sum of session durations within the trailing 7-day window (today minus six days,
    /// inclusive) using the local calendar date.
    /// </summary>
    public TimeSpan TotalActiveTimeLastSevenDays { get; init; }

    /// <summary>
    /// Most frequently completed workout name across all history.
    /// Ties are broken alphabetically. Null when the user has no history.
    /// </summary>
    public string? PreferredWorkoutName { get; init; }
}

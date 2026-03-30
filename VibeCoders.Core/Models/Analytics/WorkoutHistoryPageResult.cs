namespace VibeCoders.Models.Analytics;

/// <summary>
/// One page of workout history along with the total count for pagination controls.
/// </summary>
public sealed class WorkoutHistoryPageResult
{
    public int TotalCount { get; init; }
    public IReadOnlyList<WorkoutHistoryRow> Items { get; init; } = Array.Empty<WorkoutHistoryRow>();
}

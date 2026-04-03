namespace VibeCoders.Models.Analytics;

public sealed class WorkoutHistoryPageResult
{
    public int TotalCount { get; init; }
    public IReadOnlyList<WorkoutHistoryRow> Items { get; init; } = Array.Empty<WorkoutHistoryRow>();
}

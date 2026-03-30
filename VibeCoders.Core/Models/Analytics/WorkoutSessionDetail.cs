using System.Globalization;

namespace VibeCoders.Models.Analytics;

/// <summary>
/// Expanded workout session including per-set rows from workout_log_sets.
/// Used by the expanded workout history detail UI.
/// </summary>
public sealed class WorkoutSessionDetail
{
    public int WorkoutLogId { get; init; }
    public string WorkoutName { get; init; } = string.Empty;
    public DateTime LogDate { get; init; }
    public int DurationSeconds { get; init; }
    public IReadOnlyList<WorkoutSetRow> Sets { get; init; } = Array.Empty<WorkoutSetRow>();
}

/// <summary>
/// One set line inside the expanded workout detail view.
/// </summary>
public sealed class WorkoutSetRow
{
    public string ExerciseName { get; init; } = string.Empty;
    public int SetIndex { get; init; }
    public int? ActualReps { get; init; }
    public double? ActualWeight { get; init; }

    public string RepsDisplay =>
        ActualReps?.ToString(CultureInfo.InvariantCulture) ?? "\u2014";

    public string WeightDisplay =>
        ActualWeight.HasValue
            ? ActualWeight.Value.ToString("F1", CultureInfo.InvariantCulture)
            : "\u2014";
}

using System.Globalization;

namespace VibeCoders.Models.Analytics;

public sealed class WorkoutSessionDetail
{
    public int WorkoutLogId { get; init; }
    public string WorkoutName { get; init; } = string.Empty;
    public DateTime LogDate { get; init; }
    public int DurationSeconds { get; init; }
    public int TotalCaloriesBurned { get; init; }
    public string IntensityTag { get; init; } = string.Empty;
    public IReadOnlyList<WorkoutSetRow> Sets { get; init; } = Array.Empty<WorkoutSetRow>();
    public IReadOnlyList<ExerciseCalorieInfo> ExerciseCalories { get; init; } = Array.Empty<ExerciseCalorieInfo>();
}

public sealed class ExerciseCalorieInfo
{
    public string ExerciseName { get; init; } = string.Empty;
    public int CaloriesBurned { get; init; }
}

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

namespace VibeCoders.Models;

/// <summary>
/// A single set row linked to a workout log (mirrors the workout_log_sets table).
/// </summary>
public sealed class LoggedSet
{
    public int Id { get; set; }
    public int WorkoutLogId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int SetIndex { get; set; }
    public int? TargetReps { get; set; }
    public int? ActualReps { get; set; }
    public double? TargetWeight { get; set; }
    public double? ActualWeight { get; set; }
}

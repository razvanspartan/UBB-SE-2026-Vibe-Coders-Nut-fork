namespace VibeCoders.Models;

/// <summary>
/// An exercise recorded inside a <see cref="WorkoutLog"/>.
/// </summary>
public sealed class LoggedExercise
{
    public int Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int WorkoutLogId { get; set; }
    public float Met { get; set; }
    public List<LoggedSet> Sets { get; set; } = new();
    public int ExerciseCaloriesBurned { get; set; }
}

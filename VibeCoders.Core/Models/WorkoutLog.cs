namespace VibeCoders.Models;

/// <summary>
/// A completed workout session persisted in the workout_log table.
/// </summary>
public sealed class WorkoutLog
{
    public int Id { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Duration { get; set; }
    public int SourceTemplateId { get; set; }
    public List<LoggedExercise> Exercises { get; set; } = new();
    public int TotalCaloriesBurned { get; set; }
}

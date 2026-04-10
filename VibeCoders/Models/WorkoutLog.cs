namespace VibeCoders.Models;

public sealed class WorkoutLog
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Duration { get; set; }
    public int SourceTemplateId { get; set; }
    public WorkoutType Type { get; set; }
    public List<LoggedExercise> Exercises { get; set; } = new ();
    public int TotalCaloriesBurned { get; set; }
    public float AverageMet { get; set; }
    public string IntensityTag { get; set; } = string.Empty;
    public double Rating { get; set; } = -1;
    public string TrainerNotes { get; set; } = string.Empty;
}

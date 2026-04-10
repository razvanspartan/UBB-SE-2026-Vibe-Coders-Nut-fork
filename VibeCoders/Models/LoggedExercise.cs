namespace VibeCoders.Models;

public sealed class LoggedExercise
{
    public int Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int WorkoutLogId { get; set; }

    public int ParentTemplateExerciseId { get; set; }

    public List<LoggedSet> Sets { get; set; } = new ();
    public MuscleGroup TargetMuscles { get; set; }
    public float Met { get; set; }
    public int ExerciseCaloriesBurned { get; set; }

    public double PerformanceRatio { get; set; }

    public bool IsSystemAdjusted { get; set; }

    public string AdjustmentNote { get; set; } = string.Empty;
}
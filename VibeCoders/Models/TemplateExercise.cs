namespace VibeCoders.Models
{
    public class TemplateExercise
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int WorkoutTemplateId { get; set; }

        public MuscleGroup MuscleGroup { get; set; }

        public int TargetSets { get; set; }

        public int TargetReps { get; set; }

        public double TargetWeight { get; set; }
    }
}
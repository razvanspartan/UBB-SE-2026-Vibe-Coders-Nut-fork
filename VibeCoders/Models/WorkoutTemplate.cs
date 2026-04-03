namespace VibeCoders.Models
{
    public class WorkoutTemplate
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public WorkoutType Type { get; set; }

        private readonly List<TemplateExercise> _exercises = new();

        public void AddExercise(TemplateExercise exercise)
        {
            if (exercise == null) return;
            _exercises.Add(exercise);
        }

        public void RemoveExercise(TemplateExercise exercise)
        {
            if (exercise == null) return;
            _exercises.Remove(exercise);
        }

        public List<TemplateExercise> GetExercises()
        {
            return _exercises;
        }
    }
}
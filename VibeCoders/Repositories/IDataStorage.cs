using VibeCoders.Models;
using User = VibeCoders.Models.User;

namespace VibeCoders.Repositories
{
    public interface IDataStorage
    {
        void EnsureSchemaCreated();

        List<WorkoutTemplate> GetAvailableWorkouts(int clientId);

        TemplateExercise? GetTemplateExercise(int templateExerciseId);
        bool UpdateTemplateWeight(int templateExerciseId, double newWeight);

        List<string> GetAllExerciseNames();
    }
}
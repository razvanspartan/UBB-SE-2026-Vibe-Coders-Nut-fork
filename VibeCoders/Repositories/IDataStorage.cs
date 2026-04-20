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

        int InsertNutritionPlan(NutritionPlan plan);

        void InsertMeal(Meal meal, int nutritionPlanId);

        void AssignNutritionPlanToClient(int clientId, int nutritionPlanId);

        void SaveNutritionPlanForClient(NutritionPlan plan, int clientId);

        List<NutritionPlan> GetNutritionPlansForClient(int clientId);

        List<Meal> GetMealsForPlan(int nutritionPlanId);
    }
}
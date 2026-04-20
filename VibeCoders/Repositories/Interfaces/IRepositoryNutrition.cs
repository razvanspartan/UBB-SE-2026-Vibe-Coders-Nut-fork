namespace VibeCoders.Repositories.Interfaces
{
    using VibeCoders.Models;

    public interface IRepositoryNutrition
    {
        int InsertNutritionPlan(NutritionPlan plan);

        void InsertMeal(Meal meal, int nutritionPlanId);

        void AssignNutritionPlanToClient(int clientId, int nutritionPlanId);

        void SaveNutritionPlanForClient(NutritionPlan plan, int clientId);

        List<NutritionPlan> GetNutritionPlansForClient(int clientId);

        List<Meal> GetMealsForPlan(int nutritionPlanId);
    }
}

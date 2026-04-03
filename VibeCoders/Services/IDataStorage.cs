using VibeCoders.Models;
using User = VibeCoders.Models.User;

namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        void EnsureSchemaCreated();

        bool SaveUser(User u);
        User? LoadUser(string username);

        bool SaveClientData(Client c);
        List<Client> GetTrainerClient(int trainerId);

        List<WorkoutTemplate> GetAvailableWorkouts(int clientId);

        bool SaveWorkoutLog(WorkoutLog log);
        List<WorkoutLog> GetWorkoutHistory(int clientId);
        List<WorkoutLog> GetLastTwoLogsForExercise(int templateExerciseId);

        TemplateExercise? GetTemplateExercise(int templateExerciseId);
        bool UpdateTemplateWeight(int templateExerciseId, double newWeight);

        bool SaveNotification(Notification notification);
        List<Notification> GetNotifications(int clientId);

        List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId);

        int GetWorkoutCount(int clientId);

        int GetDistinctWorkoutDayCount(int clientId);

        AchievementShowcaseItem? GetAchievementForClient(int achievementId, int clientId);

        bool UpdateWorkoutLogFeedback(int workoutLogId, double rating, string notes);

        bool AwardAchievement(int clientId, int achievementId);

        void EvaluateAndUnlockWorkoutMilestones(int clientId);

        int GetConsecutiveWorkoutDayStreak(int clientId);

        int GetWorkoutsInLastSevenDays(int clientId);

        bool SaveTrainerWorkout(WorkoutTemplate template);
        bool DeleteWorkoutTemplate(int templateId);
        List<string> GetAllExerciseNames();

        int InsertNutritionPlan(NutritionPlan plan);

        void InsertMeal(Meal meal, int nutritionPlanId);

        void AssignNutritionPlanToClient(int clientId, int nutritionPlanId);

        void SaveNutritionPlanForClient(NutritionPlan plan, int clientId);

        List<NutritionPlan> GetNutritionPlansForClient(int clientId);

        List<Meal> GetMealsForPlan(int nutritionPlanId);

        int GetTotalActiveTimeForClient(int clientId);
    }
}

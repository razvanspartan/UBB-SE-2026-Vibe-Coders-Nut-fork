namespace VibeCoders.Repositories.Interfaces
{
    using VibeCoders.Models;

    public interface IRepositoryAchievements
    {
        int GetConsecutiveWorkoutDayStreak(int clientId);

        List<Achievement> GetAllAchievements();

        void EvaluateAndUnlockWorkoutMilestones(int clientId);

        int GetWorkoutsInLastSevenDays(int clientId);

        List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId);

        int GetWorkoutCount(int clientId);

        int GetDistinctWorkoutDayCount(int clientId);

        AchievementShowcaseItem? GetAchievementForClient(int achievementId, int clientId);

        bool AwardAchievement(int clientId, int achievementId);
    }
}

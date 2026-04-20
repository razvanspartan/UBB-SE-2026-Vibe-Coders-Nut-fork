using VibeCoders.Repositories.Interfaces;

namespace VibeCoders.Domain;

public sealed class WorkoutCountCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int Threshold { get; }

    public WorkoutCountCheck(string achievementTitle, int threshold)
    {
        AchievementTitle = achievementTitle;
        Threshold = threshold;
    }

    public bool IsMet(int clientId, IRepositoryAchievements achievementsRepository)
        => achievementsRepository.GetWorkoutCount(clientId) >= Threshold;
}

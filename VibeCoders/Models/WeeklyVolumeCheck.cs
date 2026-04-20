namespace VibeCoders.Domain;

using VibeCoders.Repositories;
using VibeCoders.Repositories.Interfaces;

public sealed class WeeklyVolumeCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int RequiredWorkoutsPerWeek { get; }

    public WeeklyVolumeCheck(string achievementTitle, int requiredWorkoutsPerWeek)
    {
        AchievementTitle = achievementTitle;
        RequiredWorkoutsPerWeek = requiredWorkoutsPerWeek;
    }

    public bool IsMet(int clientId, IRepositoryAchievements achievementsRepository)
        => achievementsRepository.GetWorkoutsInLastSevenDays(clientId) >= RequiredWorkoutsPerWeek;
}

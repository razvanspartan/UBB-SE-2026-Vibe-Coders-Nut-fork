using VibeCoders.Services;

namespace VibeCoders.Domain;

public sealed class WeeklyVolumeCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int RequiredWorkoutsPerWeek { get; }

    public WeeklyVolumeCheck(string achievementTitle, int requiredWorkoutsPerWeek)
    {
        AchievementTitle = achievementTitle;
        RequiredWorkoutsPerWeek = requiredWorkoutsPerWeek;
    }

    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetWorkoutsInLastSevenDays(clientId) >= RequiredWorkoutsPerWeek;
}

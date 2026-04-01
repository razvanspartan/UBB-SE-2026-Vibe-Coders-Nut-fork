using VibeCoders.Services;

namespace VibeCoders.Domain;

public sealed class WeeklyVolumeCheck : IMilestoneCheck
{
    private readonly int _threshold;

    public string AchievementTitle { get; }

    public WeeklyVolumeCheck(string achievementTitle, int requiredWorkoutsPerWeek)
    {
        AchievementTitle = achievementTitle;
        _threshold = requiredWorkoutsPerWeek;
    }

    public bool IsMet(int clientId, IDataStorage storage)
    {
        // This calls the SQL method you already built!
        int weeklyWorkouts = storage.GetWorkoutsInLastSevenDays(clientId);
        return weeklyWorkouts >= _threshold;
    }
}
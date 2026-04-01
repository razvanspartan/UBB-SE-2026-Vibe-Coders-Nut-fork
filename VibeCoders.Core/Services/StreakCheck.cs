using VibeCoders.Services;

namespace VibeCoders.Services;

public sealed class StreakCheck : IMilestoneCheck
{
    private readonly int _requiredDays;

    public string AchievementTitle { get; }

    public StreakCheck(string achievementTitle, int requiredConsecutiveDays)
    {
        AchievementTitle = achievementTitle;
        _requiredDays = requiredConsecutiveDays;
    }

    public bool IsMet(int clientId, IDataStorage storage)
    {
        return storage.GetConsecutiveWorkoutDayStreak(clientId) >= _requiredDays;
    }
}

using VibeCoders.Services;

namespace VibeCoders.Domain;

public sealed class StreakCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int RequiredConsecutiveDays { get; }

    public StreakCheck(string achievementTitle, int requiredConsecutiveDays)
    {
        AchievementTitle       = achievementTitle;
        RequiredConsecutiveDays = requiredConsecutiveDays;
    }

    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetConsecutiveWorkoutDayStreak(clientId) >= RequiredConsecutiveDays;
}

namespace VibeCoders.Domain;

using VibeCoders.Services;

public sealed class StreakCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }

    public int RequiredConsecutiveDays { get; }

    public StreakCheck(string achievementTitle, int requiredConsecutiveDays)
    {
        this.AchievementTitle = achievementTitle;
        this.RequiredConsecutiveDays = requiredConsecutiveDays;
    }

    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetConsecutiveWorkoutDayStreak(clientId) >= this.RequiredConsecutiveDays;
}

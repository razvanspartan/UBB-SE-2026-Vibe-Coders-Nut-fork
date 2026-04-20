namespace VibeCoders.Domain;

using VibeCoders.Repositories.Interfaces;

public sealed class StreakCheck : IMilestoneCheck
{
    private readonly IRepositoryAchievements achievementRepository;
    public string AchievementTitle { get; }

    public int RequiredConsecutiveDays { get; }

    public StreakCheck(string achievementTitle, int requiredConsecutiveDays)
    {
        this.AchievementTitle = achievementTitle;
        this.RequiredConsecutiveDays = requiredConsecutiveDays;
    }

    public bool IsMet(int clientId, IRepositoryAchievements achievmentsRepository)
        => achievmentsRepository.GetConsecutiveWorkoutDayStreak(clientId) >= this.RequiredConsecutiveDays;
}

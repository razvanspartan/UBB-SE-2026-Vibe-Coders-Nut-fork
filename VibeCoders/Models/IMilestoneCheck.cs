using VibeCoders.Repositories.Interfaces;

namespace VibeCoders.Domain;

public interface IMilestoneCheck
{
    string AchievementTitle { get; }

    bool IsMet(int clientId, IRepositoryAchievements achievementRepository);
}

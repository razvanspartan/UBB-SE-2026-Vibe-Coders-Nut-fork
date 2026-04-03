using VibeCoders.Services;

namespace VibeCoders.Domain;

public interface IMilestoneCheck
{
    string AchievementTitle { get; }

    bool IsMet(int clientId, IDataStorage storage);
}

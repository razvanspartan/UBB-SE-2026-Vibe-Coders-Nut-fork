namespace VibeCoders.Domain;

using VibeCoders.Repositories.Interfaces;
public sealed class FirstWorkoutCheck : IMilestoneCheck
{
    public string AchievementTitle => "First Step";

    public bool IsMet(int clientId, IRepositoryAchievements storage)
    {
        return storage.GetWorkoutCount(clientId) > 0;
    }
}

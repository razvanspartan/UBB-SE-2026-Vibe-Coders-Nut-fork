using VibeCoders.Services;

namespace VibeCoders.Services;

public sealed class FirstWorkoutCheck : IMilestoneCheck
{
    public string AchievementTitle => "First Step";

    public bool IsMet(int clientId, IDataStorage storage)
    {
        return storage.GetWorkoutCount(clientId) > 0;
    }
}

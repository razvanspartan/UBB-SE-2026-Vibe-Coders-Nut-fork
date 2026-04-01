using VibeCoders.Services;

namespace VibeCoders.Services;

public sealed class WorkoutCountCheck : IMilestoneCheck
{
    private readonly int _threshold;

    public string AchievementTitle { get; }

    public WorkoutCountCheck(string achievementTitle, int threshold)
    {
        AchievementTitle = achievementTitle;
        _threshold = threshold;
    }

    public bool IsMet(int clientId, IDataStorage storage)
    {
        return storage.GetWorkoutCount(clientId) >= _threshold;
    }
}

using VibeCoders.Services;

namespace VibeCoders.Services;

public sealed class FirstWorkoutCheck : IMilestoneCheck
{
    public string AchievementName => "First Step";

    public bool Evaluate(int userId, IDataStorage storage)
    {
        var logs = storage.GetWorkoutLogs(userId);
        return logs.Any();
    }
}

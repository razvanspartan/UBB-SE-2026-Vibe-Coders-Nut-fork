using VibeCoders.Services;

namespace VibeCoders.Services;

public sealed class WorkoutCountCheck : IMilestoneCheck
{
    private readonly int _threshold;

    public string AchievementName { get; }

    public WorkoutCountCheck(string achievementName, int threshold)
    {
        AchievementName = achievementName;
        _threshold = threshold;
    }

    public bool Evaluate(int userId, IDataStorage storage)
    {
        var logs = storage.GetWorkoutLogs(userId);
        return logs.Count() >= _threshold;
    }
}

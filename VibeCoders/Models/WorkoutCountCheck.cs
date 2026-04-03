using VibeCoders.Services;

namespace VibeCoders.Domain;

public sealed class WorkoutCountCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int Threshold { get; }

    public WorkoutCountCheck(string achievementTitle, int threshold)
    {
        AchievementTitle = achievementTitle;
        Threshold        = threshold;
    }

    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetWorkoutCount(clientId) >= Threshold;
}

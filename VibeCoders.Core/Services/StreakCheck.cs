using VibeCoders.Services;

namespace VibeCoders.Services;

public sealed class StreakCheck : IMilestoneCheck
{
    private readonly int _requiredDays;

    public string AchievementName { get; }

    public StreakCheck(string achievementName, int requiredDays)
    {
        AchievementName = achievementName;
        _requiredDays = requiredDays;
    }

    public bool Evaluate(int userId, IDataStorage storage)
    {
        var distinctDays = storage
            .GetWorkoutLogs(userId)
            .Select(l => l.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (distinctDays.Count < _requiredDays) return false;

        // Walk backwards from the most recent day counting consecutive days
        int streak = 1;
        for (int i = 0; i < distinctDays.Count - 1; i++)
        {
            if ((distinctDays[i] - distinctDays[i + 1]).Days == 1)
            {
                streak++;
                if (streak >= _requiredDays) return true;
            }
            else
            {
                break; // streak broken
            }
        }

        return streak >= _requiredDays;
    }
}

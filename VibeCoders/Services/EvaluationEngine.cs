using System.Diagnostics;
using VibeCoders.Domain;
using VibeCoders.Models;

namespace VibeCoders.Services;

public sealed class EvaluationEngine
{
    private readonly IDataStorage _storage;
    private readonly IReadOnlyList<VibeCoders.Domain.IMilestoneCheck> _checks;

    public EvaluationEngine(IDataStorage storage) : this(storage, BuildDefaultChecks()) { }

    public EvaluationEngine(IDataStorage storage, IReadOnlyList<VibeCoders.Domain.IMilestoneCheck> checks)
    {
        _storage = storage;
        _checks  = checks;
    }

    private static IReadOnlyList<VibeCoders.Domain.IMilestoneCheck> BuildDefaultChecks()
    {
        var checks = TotalWorkoutsMilestoneEvaluator.DefaultMilestones
            .Select(m => (VibeCoders.Domain.IMilestoneCheck)new VibeCoders.Domain.WorkoutCountCheck(m.Title, m.Threshold))
            .ToList();

        checks.Add(new VibeCoders.Domain.StreakCheck("3-Day Streak", requiredConsecutiveDays: 3));
        checks.Add(new VibeCoders.Domain.StreakCheck("Week Warrior",  requiredConsecutiveDays: 7));

        checks.Add(new VibeCoders.Domain.WeeklyVolumeCheck("Week Champion", requiredWorkoutsPerWeek: 6));

        return checks;
    }

    public IReadOnlyList<string> Evaluate(int clientId)
    {
        var newlyUnlocked = new List<string>();

        try
        {
            var catalog = _storage
                .GetAchievementShowcaseForClient(clientId)
                .ToDictionary(a => a.Title, StringComparer.OrdinalIgnoreCase);

            foreach (var check in _checks)
            {
                if (!catalog.TryGetValue(check.AchievementTitle, out var item)) continue;
                if (item.IsUnlocked) continue;

                if (!check.IsMet(clientId, _storage)) continue;

                bool awarded = _storage.AwardAchievement(clientId, item.AchievementId);
                if (!awarded) continue;

                newlyUnlocked.Add(check.AchievementTitle);
                Debug.WriteLine(
                    $"[EvaluationEngine] Unlocked '{check.AchievementTitle}' for client {clientId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[EvaluationEngine] Evaluation error for client {clientId}: {ex.Message}");
        }

        return newlyUnlocked;
    }
}

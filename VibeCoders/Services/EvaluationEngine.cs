using System.Diagnostics;
using VibeCoders.Domain;
using VibeCoders.Models;

namespace VibeCoders.Services;

public sealed class EvaluationEngine
{
    private readonly IDataStorage storage;
    private readonly IReadOnlyList<VibeCoders.Domain.IMilestoneCheck> checks;

    public EvaluationEngine(IDataStorage storage) : this(storage, BuildDefaultChecks())
    {
    }

    public EvaluationEngine(IDataStorage storage, IReadOnlyList<VibeCoders.Domain.IMilestoneCheck> checks)
    {
        this.storage = storage;
        this.checks = checks;
    }

    private static IReadOnlyList<VibeCoders.Domain.IMilestoneCheck> BuildDefaultChecks()
    {
        var checks = TotalWorkoutsMilestoneEvaluator.DefaultMilestones
            .Select(m => (VibeCoders.Domain.IMilestoneCheck)new VibeCoders.Domain.WorkoutCountCheck(m.title, m.threshold))
            .ToList();

        checks.Add(new VibeCoders.Domain.StreakCheck("3-Day Streak", requiredConsecutiveDays: 3));
        checks.Add(new VibeCoders.Domain.StreakCheck("Week Warrior",  requiredConsecutiveDays: 7));

        checks.Add(new VibeCoders.Domain.WeeklyVolumeCheck("Iron Week", requiredWorkoutsPerWeek: 5));

        return checks;
    }

    public IReadOnlyList<string> Evaluate(int clientId)
    {
        var newlyUnlocked = new List<string>();

        try
        {
            var catalog = storage
                .GetAchievementShowcaseForClient(clientId)
                .ToDictionary(a => a.Title, StringComparer.OrdinalIgnoreCase);

            foreach (var check in checks)
            {
                if (!catalog.TryGetValue(check.AchievementTitle, out var item))
                {
                    continue;
                }

                if (item.IsUnlocked)
                {
                    continue;
                }

                if (!check.IsMet(clientId, storage))
                {
                    continue;
                }

                bool awarded = storage.AwardAchievement(clientId, item.AchievementId);
                if (!awarded)
                {
                    continue;
                }

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

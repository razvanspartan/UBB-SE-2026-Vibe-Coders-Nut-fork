namespace VibeCoders.Services;

using System.Diagnostics;
using VibeCoders.Domain;
using VibeCoders.Models;

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

    public RankShowcaseSnapshot BuildRankShowcase(int clientId)
    {
        var showcase = storage.GetAchievementShowcaseForClient(clientId);
        int unlockedCount = showcase.Count(item => item.IsUnlocked);

        var tiers = LevelingTierEvaluator.DefaultTiers;
        var result = LevelingTierEvaluator.Evaluate(unlockedCount, tiers);
        var progress = ComputeNextRankProgress(unlockedCount, tiers, result.level);

        return new RankShowcaseSnapshot
        {
            DisplayLevel = result.level,
            RankTitle = result.rankTitle,
            UnlockedAchievementsDisplay =
                $"{unlockedCount} achievement{(unlockedCount == 1 ? string.Empty : "s")} unlocked",
            LevelDisplayLine = $"Level {result.level}: {result.rankTitle}",
            HasNextRank = progress.HasNextRank,
            ProgressPercent = progress.ProgressPercent,
            NextRankInfo = progress.NextRankInfo,
            ShowcaseAchievements = showcase,
        };
    }

    private static (bool HasNextRank, double ProgressPercent, string NextRankInfo) ComputeNextRankProgress(
        int unlockedCount,
        IReadOnlyList<LevelTier> tiers,
        int currentLevel)
    {
        int currentIndex = -1;
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].level == currentLevel)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex + 1;
        if (currentIndex < 0 || nextIndex >= tiers.Count)
        {
            return (false, 100, "Max rank reached — keep going!");
        }

        var current = tiers[currentIndex];
        var next = tiers[nextIndex];

        int bandStart = current.minimumAchievements;
        int bandEnd = next.minimumAchievements;
        int earned = unlockedCount - bandStart;
        int needed = bandEnd - bandStart;

        double progressPercent = needed > 0
            ? Math.Min(100, Math.Round(earned * 100.0 / needed, 1))
            : 100;

        int remaining = Math.Max(0, bandEnd - unlockedCount);
        string nextRankInfo =
            $"Next: Level {next.level}: {next.rankTitle} — {remaining} more achievement{(remaining == 1 ? string.Empty : "s")} to go";

        return (true, progressPercent, nextRankInfo);
    }
}

public sealed class RankShowcaseSnapshot
{
    public int DisplayLevel { get; init; }

    public string RankTitle { get; init; } = "—";

    public string UnlockedAchievementsDisplay { get; init; } = "0 achievements unlocked";

    public string LevelDisplayLine { get; init; } = "Level —";

    public double ProgressPercent { get; init; }

    public string NextRankInfo { get; init; } = string.Empty;

    public bool HasNextRank { get; init; }

    public IReadOnlyList<AchievementShowcaseItem> ShowcaseAchievements { get; init; } = Array.Empty<AchievementShowcaseItem>();
}

namespace VibeCoders.Domain;

using System.Globalization;

public static class LevelingTierEvaluator
{
    public static IReadOnlyList<LevelTier> DefaultTiers { get; } =
    [
        new LevelTier(1, "Beginner",       0),
        new LevelTier(2, "Trainee",        1),
        new LevelTier(3, "Apprentice",     2),
        new LevelTier(4, "Gym Novice",     3),
        new LevelTier(5, "Gym Enthusiast", 5),
        new LevelTier(6, "Athlete",        7),
        new LevelTier(7, "Elite",         10),
    ];

    public static LevelingResult Evaluate(int unlockedAchievements, IReadOnlyList<LevelTier>? tiers = null)
    {
        tiers ??= DefaultTiers;
        if (tiers.Count == 0)
        {
            return new LevelingResult(0, "Unranked");
        }

        LevelTier? best = null;
        foreach (var tier in tiers)
        {
            if (unlockedAchievements >= tier.minimumAchievements)
            {
                best = tier;
            }
        }

        return best is null
            ? new LevelingResult(0, "Unranked")
            : new LevelingResult(best.Value.level, best.Value.rankTitle);
    }
}

public readonly record struct LevelTier(int level, string rankTitle, int minimumAchievements)
{
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture,
            "Level {0} {1} @ {2} achievements", level, rankTitle, minimumAchievements);
}

public readonly record struct LevelingResult(int level, string rankTitle);

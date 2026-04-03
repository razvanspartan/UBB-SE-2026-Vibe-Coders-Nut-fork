using System.Globalization;

namespace VibeCoders.Domain;

public static class LevelingTierEvaluator
{
    public static IReadOnlyList<LevelTier> DefaultTiers { get; } =
    [
        new LevelTier(1, "Novice", 0),
        new LevelTier(2, "Beginner", (long)TimeSpan.FromHours(1).TotalSeconds),
        new LevelTier(3, "Regular", (long)TimeSpan.FromHours(10).TotalSeconds),
        new LevelTier(4, "Dedicated", (long)TimeSpan.FromHours(50).TotalSeconds),
        new LevelTier(5, "Elite", (long)TimeSpan.FromHours(200).TotalSeconds),
    ];

    public static LevelingResult Evaluate(TimeSpan totalActiveTime, IReadOnlyList<LevelTier>? tiers = null)
    {
        tiers ??= DefaultTiers;
        if (tiers.Count == 0)
        {
            return new LevelingResult(0, "Unranked");
        }

        var seconds = (long)Math.Max(0, totalActiveTime.TotalSeconds);

        LevelTier? best = null;
        foreach (var tier in tiers)
        {
            if (seconds >= tier.MinTotalSeconds)
            {
                best = tier;
            }
        }

        return best is null
            ? new LevelingResult(0, "Unranked")
            : new LevelingResult(best.Value.Level, best.Value.RankTitle);
    }
}

public readonly record struct LevelTier(int Level, string RankTitle, long MinTotalSeconds)
{
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture,
            "Level {0} {1} @ {2}s", Level, RankTitle, MinTotalSeconds);
}

public readonly record struct LevelingResult(int Level, string RankTitle);

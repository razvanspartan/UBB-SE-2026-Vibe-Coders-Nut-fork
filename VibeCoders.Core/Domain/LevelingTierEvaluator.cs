using System.Globalization;

namespace VibeCoders.Domain;

/// <summary>
/// Maps cumulative active workout time to a numeric level and display rank.
/// Tiers are inclusive: the highest tier whose <see cref="LevelTier.MinTotalSeconds"/>
/// is less than or equal to the user's total time applies.
/// </summary>
public static class LevelingTierEvaluator
{
    /// <summary>
    /// Default progression table (total active time thresholds). Adjust here or
    /// inject a custom list via <see cref="Evaluate"/>.
    /// </summary>
    /// <remarks>
    /// Level 1 starts at 0 seconds. Each row is the minimum total time required
    /// to display that level and rank.
    /// </remarks>
    public static IReadOnlyList<LevelTier> DefaultTiers { get; } =
    [
        new LevelTier(1, "Novice", 0),
        new LevelTier(2, "Beginner", (long)TimeSpan.FromHours(1).TotalSeconds),
        new LevelTier(3, "Regular", (long)TimeSpan.FromHours(10).TotalSeconds),
        new LevelTier(4, "Dedicated", (long)TimeSpan.FromHours(50).TotalSeconds),
        new LevelTier(5, "Elite", (long)TimeSpan.FromHours(200).TotalSeconds),
    ];

    /// <summary>
    /// Computes level and rank title for the given total active time.
    /// </summary>
    /// <param name="totalActiveTime">Cumulative duration; negative values are treated as zero.</param>
    /// <param name="tiers">Non-empty ordered tiers (typically ascending <see cref="LevelTier.MinTotalSeconds"/>).</param>
    /// <returns>Result for the highest matching tier, or level 0 / "Unranked" if <paramref name="tiers"/> is empty.</returns>
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

/// <summary>
/// One row in the leveling table: minimum cumulative active seconds, level number, and rank label.
/// </summary>
public readonly record struct LevelTier(int Level, string RankTitle, long MinTotalSeconds)
{
    /// <inheritdoc />
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture,
            "Level {0} {1} @ {2}s", Level, RankTitle, MinTotalSeconds);
}

/// <summary>
/// Output of <see cref="LevelingTierEvaluator.Evaluate"/>.
/// </summary>
public readonly record struct LevelingResult(int Level, string RankTitle);

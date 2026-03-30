using VibeCoders.Domain;

namespace VibeCoders.Tests;

public sealed class LevelingTierEvaluatorTests
{
    private static readonly IReadOnlyList<LevelTier> TestTiers =
    [
        new LevelTier(1, "A", 0),
        new LevelTier(2, "B", 100),
        new LevelTier(3, "C", 500),
    ];

    [Fact]
    public void Evaluate_zero_seconds_matches_first_tier()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.Zero, TestTiers);
        Assert.Equal(1, r.Level);
        Assert.Equal("A", r.RankTitle);
    }

    [Fact]
    public void Evaluate_exactly_on_boundary_selects_that_tier()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.FromSeconds(100), TestTiers);
        Assert.Equal(2, r.Level);
        Assert.Equal("B", r.RankTitle);
    }

    [Fact]
    public void Evaluate_past_top_tier_selects_highest()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.FromSeconds(10_000), TestTiers);
        Assert.Equal(3, r.Level);
        Assert.Equal("C", r.RankTitle);
    }

    [Fact]
    public void Evaluate_negative_time_treated_as_zero()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.FromSeconds(-10), TestTiers);
        Assert.Equal(1, r.Level);
        Assert.Equal("A", r.RankTitle);
    }

    [Fact]
    public void Evaluate_empty_tiers_returns_unranked()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.FromHours(1), Array.Empty<LevelTier>());
        Assert.Equal(0, r.Level);
        Assert.Equal("Unranked", r.RankTitle);
    }

    [Fact]
    public void Evaluate_between_tiers_selects_lower()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.FromSeconds(150), TestTiers);
        Assert.Equal(2, r.Level);
        Assert.Equal("B", r.RankTitle);
    }

    [Fact]
    public void DefaultTiers_returns_sensible_novice_at_zero()
    {
        var r = LevelingTierEvaluator.Evaluate(TimeSpan.Zero);
        Assert.Equal(1, r.Level);
        Assert.Equal("Novice", r.RankTitle);
    }
}

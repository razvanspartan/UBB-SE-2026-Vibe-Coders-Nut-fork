using VibeCoders.Models;

namespace VibeCoders.Tests;

public sealed class AchievementShowcaseItemTests
{
    [Fact]
    public void AchievementShowcaseItem_round_trips_properties()
    {
        var row = new AchievementShowcaseItem
        {
            AchievementId = 42,
            Title = "First Steps",
            Description = "Complete your first workout.",
            IsUnlocked = true
        };

        Assert.Equal(42, row.AchievementId);
        Assert.Equal("First Steps", row.Title);
        Assert.Equal("Complete your first workout.", row.Description);
        Assert.True(row.IsUnlocked);
        Assert.Equal("Unlocked", row.StatusLine);

        var locked = new AchievementShowcaseItem
        {
            AchievementId = 1,
            Title = "T",
            Description = "D",
            IsUnlocked = false
        };
        Assert.Equal("Locked", locked.StatusLine);
    }
}

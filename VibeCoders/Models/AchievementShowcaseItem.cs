namespace VibeCoders.Models;

public sealed class AchievementShowcaseItem
{
    public int AchievementId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Criteria { get; init; } = string.Empty;

    public bool IsUnlocked { get; init; }

    public bool IsLocked => !IsUnlocked;

    public string StatusLine => IsUnlocked ? "Unlocked" : "Locked";
}

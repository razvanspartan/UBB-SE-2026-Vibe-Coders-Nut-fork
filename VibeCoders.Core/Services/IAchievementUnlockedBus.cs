using VibeCoders.Models;

namespace VibeCoders.Services;

/// Event args carrying the details of a newly unlocked achievement.

public sealed class AchievementUnlockedEventArgs : EventArgs
{
    public AchievementShowcaseItem Achievement { get; }

    public AchievementUnlockedEventArgs(AchievementShowcaseItem achievement)
    {
        Achievement = achievement;
    }
}


/// Cross-feature event bus: the evaluation engine publishes when a badge is
/// newly awarded; the UI layer subscribes to trigger the unlock popup.

public interface IAchievementUnlockedBus
{
    event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;

    void NotifyUnlocked(AchievementShowcaseItem achievement);
}

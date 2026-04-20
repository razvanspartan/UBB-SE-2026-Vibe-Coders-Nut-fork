using VibeCoders.Models;

namespace VibeCoders.Services;

public sealed class AchievementUnlockedEventArgs : EventArgs
{
    public AchievementShowcaseItem Achievement { get; }

    public AchievementUnlockedEventArgs(AchievementShowcaseItem achievement)
    {
        Achievement = achievement;
    }
}

public interface IAchievementUnlockedBus
{
    event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;

    void NotifyUnlocked(AchievementShowcaseItem achievement);
}
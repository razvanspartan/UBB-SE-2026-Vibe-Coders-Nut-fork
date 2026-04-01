using VibeCoders.Models;

namespace VibeCoders.Services;

/// <inheritdoc cref="IAchievementUnlockedBus" />
public sealed class AchievementUnlockedBus : IAchievementUnlockedBus
{
    public event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;

    public void NotifyUnlocked(AchievementShowcaseItem achievement)
    {
        AchievementUnlocked?.Invoke(this, new AchievementUnlockedEventArgs(achievement));
    }
}

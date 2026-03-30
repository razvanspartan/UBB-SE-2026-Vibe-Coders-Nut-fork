namespace VibeCoders.Models;

/// <summary>
/// One row in the rank achievement showcase: definition from <c>ACHIEVEMENT</c> plus the
/// client&apos;s unlock state from <c>CLIENT_ACHIEVEMENT</c>. Locked entries are still emitted
/// so the UI can list upcoming goals, not only completed ones.
/// </summary>
public sealed class AchievementShowcaseItem
{
    /// <summary>Primary key from <c>ACHIEVEMENT.achievement_id</c>.</summary>
    public int AchievementId { get; init; }

    /// <summary>Short name shown in the showcase card header.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Longer copy explaining what the player must do to earn it.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// <see langword="true"/> when <c>CLIENT_ACHIEVEMENT.unlocked = 1</c> for this client;
    /// <see langword="false"/> when there is no row or the row is still locked.
    /// </summary>
    public bool IsUnlocked { get; init; }

    /// <summary>
    /// Compact label bound in XAML (&quot;Unlocked&quot; / &quot;Locked&quot;) so the template
    /// does not need a boolean-to-visibility converter.
    /// </summary>
    public string StatusLine => IsUnlocked ? "Unlocked" : "Locked";
}

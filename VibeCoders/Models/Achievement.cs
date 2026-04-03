namespace VibeCoders.Models;

public class Achievement
{
    public int AchievementId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public int? ThresholdWorkouts { get; set; }

    public bool IsUnlocked { get; set; }
}

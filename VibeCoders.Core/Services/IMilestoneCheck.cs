namespace VibeCoders.Services;

public interface IMilestoneCheck
{
    /// <summary>
    /// Must match the achievement title in the ACHIEVEMENT catalog.
    /// </summary>
    string AchievementTitle { get; }

    /// <summary>
    /// Returns true when the client has met this milestone.
    /// </summary>
    bool IsMet(int clientId, IDataStorage storage);
}

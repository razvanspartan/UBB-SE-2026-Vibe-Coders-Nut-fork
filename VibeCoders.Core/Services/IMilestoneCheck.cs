namespace VibeCoders.Services;

public interface IMilestoneCheck
{
    /// <summary>
    /// The achievement name this check maps to.
    /// Must match Achievement.Name in your data store.
    /// </summary>
    string AchievementName { get; }

    /// <summary>
    /// Returns true if the user has met this milestone.
    /// </summary>
    bool Evaluate(int userId, IDataStorage storage);
}

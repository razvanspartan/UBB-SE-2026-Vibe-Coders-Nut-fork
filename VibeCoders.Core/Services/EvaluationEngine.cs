using System;
using System.Collections.Generic;
using System.Diagnostics;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.Services;

/// <summary>
/// Core achievement evaluation engine.
/// Call EvaluateAsync(userId) after any action that could unlock a milestone
/// (e.g. at the end of FinalizeWorkout).
/// </summary>
public sealed class EvaluationEngine
{
    private readonly IDataStorage _storage;
    private readonly IReadOnlyList<IMilestoneCheck> _checks;

    public EvaluationEngine(IDataStorage storage)
    {
        _storage = storage;

        // Register all milestone checks here.
        // Add new checks to this list as the app grows.
        _checks = new List<IMilestoneCheck>
        {
            new FirstWorkoutCheck(),

            new WorkoutCountCheck("Committed",       threshold: 10),
            new WorkoutCountCheck("Dedicated",       threshold: 25),
            new WorkoutCountCheck("Century Club",    threshold: 100),

            new StreakCheck("Week Warrior",   requiredDays: 7),
            new StreakCheck("Iron Fortnight", requiredDays: 14),
        };
    }

    /// <summary>
    /// Runs all milestone checks for the given user and unlocks any
    /// achievements they have newly earned. Returns the list of
    /// newly unlocked achievement names so the caller can notify the UI.
    /// </summary>
    public IReadOnlyList<string> Evaluate(int userId)
    {
        var newlyUnlocked = new List<string>();

        try
        {
            var existingAchievements = _storage.GetAchievements(userId);

            foreach (var check in _checks)
            {
                var achievement = existingAchievements
                    .FirstOrDefault(a => a.Name == check.AchievementName);

                // Skip if already unlocked
                if (achievement?.IsUnlocked == true) continue;

                bool earned = check.Evaluate(userId, _storage);
                if (!earned) continue;

                // Unlock it
                if (achievement != null)
                {
                    achievement.IsUnlocked = true;
                    _storage.SaveAchievement(userId, achievement);
                }
                else
                {
                    // Achievement record doesn't exist yet — create it
                    _storage.SaveAchievement(userId, new Achievement
                    {
                        Name = check.AchievementName,
                        IsUnlocked = true
                    });
                }

                newlyUnlocked.Add(check.AchievementName);
                Debug.WriteLine($"[EvaluationEngine] Unlocked '{check.AchievementName}' for user {userId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EvaluationEngine] Error during evaluation: {ex.Message}");
        }

        return newlyUnlocked;
    }
}

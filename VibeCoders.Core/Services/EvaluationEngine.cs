using System.Diagnostics;
using VibeCoders.Domain;
using VibeCoders.Models;

namespace VibeCoders.Services;

/// <summary>
/// Core background evaluation engine for achievements (#191).
///
/// After every finalized workout call <see cref="Evaluate"/> with the client ID.
/// The engine iterates all registered <see cref="IMilestoneCheck"/> rules, looks up
/// each matching achievement by title in the DB catalog, and awards it exactly once
/// per user account (idempotent — <see cref="IDataStorage.AwardAchievement"/> is a no-op
/// when the badge is already unlocked).
///
/// Adding a new achievement type:
///   1. Seed a row in <c>ACHIEVEMENT</c>.
///   2. Implement <see cref="IMilestoneCheck"/>.
///   3. Register it in <see cref="BuildDefaultChecks"/> — no other changes needed.
/// </summary>
public sealed class EvaluationEngine
{
    private readonly IDataStorage _storage;
    private readonly IReadOnlyList<IMilestoneCheck> _checks;

    /// <summary>Production constructor — uses the full default check registry.</summary>
    public EvaluationEngine(IDataStorage storage) : this(storage, BuildDefaultChecks()) { }

    /// <summary>Test constructor — accepts a custom check list for unit testing.</summary>
    public EvaluationEngine(IDataStorage storage, IReadOnlyList<IMilestoneCheck> checks)
    {
        _storage = storage;
        _checks  = checks;
    }

    // ── Default milestone registry ───────────────────────────────────────────
    // Titles must match ACHIEVEMENT.title values seeded at startup.

    private static IReadOnlyList<IMilestoneCheck> BuildDefaultChecks()
    {
        // Drive workout-count checks directly from the canonical milestone table so
        // the titles here always match what SeedWorkoutMilestoneAchievements seeds.
        var checks = TotalWorkoutsMilestoneEvaluator.DefaultMilestones
            .Select(m => (IMilestoneCheck)new WorkoutCountCheck(m.Title, m.Threshold))
            .ToList();

        // Consecutive-day streak milestones
        checks.Add(new StreakCheck("3-Day Streak", requiredConsecutiveDays: 3));
        checks.Add(new StreakCheck("Week Warrior",  requiredConsecutiveDays: 7));

        // Weekly volume milestone
        checks.Add(new WeeklyVolumeCheck("Week Champion", requiredWorkoutsPerWeek: 6));

        return checks;
    }

    // ── Public entry point ───────────────────────────────────────────────────

    /// <summary>
    /// Runs every registered milestone check for <paramref name="clientId"/>.
    /// Loads the achievement catalog once, then for each check that passes and
    /// whose achievement is not yet unlocked, awards it via
    /// <see cref="IDataStorage.AwardAchievement"/>.
    /// </summary>
    /// <param name="clientId">The client to evaluate.</param>
    /// <returns>
    /// Titles of achievements newly unlocked in this call.
    /// Empty when nothing new was earned or on error.
    /// </returns>
    public IReadOnlyList<string> Evaluate(int clientId)
    {
        var newlyUnlocked = new List<string>();

        try
        {
            // Load the full catalog once to avoid N+1 hits inside the loop.
            var catalog = _storage
                .GetAchievementShowcaseForClient(clientId)
                .ToDictionary(a => a.Title, StringComparer.OrdinalIgnoreCase);

            foreach (var check in _checks)
            {
                // Skip if the achievement is missing from the catalog (not seeded yet)
                // or is already unlocked for this client.
                if (!catalog.TryGetValue(check.AchievementTitle, out var item)) continue;
                if (item.IsUnlocked) continue;

                if (!check.IsMet(clientId, _storage)) continue;

                bool awarded = _storage.AwardAchievement(clientId, item.AchievementId);
                if (!awarded) continue;

                newlyUnlocked.Add(check.AchievementTitle);
                Debug.WriteLine(
                    $"[EvaluationEngine] Unlocked '{check.AchievementTitle}' for client {clientId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[EvaluationEngine] Evaluation error for client {clientId}: {ex.Message}");
        }

        return newlyUnlocked;
    }
}

namespace VibeCoders.Domain;

/// <summary>
/// Pure-logic class for the "Total Workouts" milestone system (#186).
/// Defines the threshold table and determines which milestones a client
/// has earned based solely on their lifetime workout count.
/// No database or UI dependencies — fully unit-testable.
/// </summary>
public sealed class TotalWorkoutsMilestoneEvaluator
{
    /// <summary>
    /// The canonical milestone table, ordered ascending by threshold.
    /// These titles must match the rows inserted by
    /// <c>SqlDataStorage.SeedWorkoutMilestoneAchievements</c>.
    /// </summary>
    public static IReadOnlyList<WorkoutMilestone> DefaultMilestones { get; } =
    [
        new WorkoutMilestone(Threshold: 1,   Title: "First Rep",       Description: "Complete your first workout."),
        new WorkoutMilestone(Threshold: 10,  Title: "Getting Serious", Description: "Complete 10 total workouts."),
        new WorkoutMilestone(Threshold: 25,  Title: "Gym Regular",     Description: "Complete 25 total workouts."),
        new WorkoutMilestone(Threshold: 50,  Title: "Iron Warrior",    Description: "Complete 50 total workouts."),
        new WorkoutMilestone(Threshold: 100, Title: "Gym Legend",      Description: "Complete 100 total workouts."),
    ];

    private readonly IReadOnlyList<WorkoutMilestone> _milestones;

    /// <summary>Creates an evaluator using the <see cref="DefaultMilestones"/> table.</summary>
    public TotalWorkoutsMilestoneEvaluator() : this(DefaultMilestones) { }

    /// <summary>Creates an evaluator with a custom milestone table (useful for tests).</summary>
    public TotalWorkoutsMilestoneEvaluator(IReadOnlyList<WorkoutMilestone> milestones)
    {
        _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
    }

    /// <summary>
    /// Returns all milestones whose threshold the client has reached or passed.
    /// The result is ordered by threshold ascending.
    /// </summary>
    /// <param name="lifetimeWorkoutCount">
    /// Number of completed workouts logged by the client (must be &gt;= 0).
    /// </param>
    public IReadOnlyList<WorkoutMilestone> GetEarnedMilestones(int lifetimeWorkoutCount)
    {
        if (lifetimeWorkoutCount < 0)
            throw new ArgumentOutOfRangeException(nameof(lifetimeWorkoutCount), "Count cannot be negative.");

        return _milestones
            .Where(m => lifetimeWorkoutCount >= m.Threshold)
            .OrderBy(m => m.Threshold)
            .ToList();
    }

    /// <summary>
    /// Returns every milestone that is newly crossed when the count moves from
    /// <paramref name="previousCount"/> to <paramref name="newCount"/>.
    /// Useful for showing a "You just earned …" notification.
    /// </summary>
    public IReadOnlyList<WorkoutMilestone> GetNewlyEarnedMilestones(int previousCount, int newCount)
    {
        if (previousCount < 0) throw new ArgumentOutOfRangeException(nameof(previousCount));
        if (newCount < previousCount) throw new ArgumentOutOfRangeException(nameof(newCount), "New count cannot be less than previous count.");

        return _milestones
            .Where(m => m.Threshold > previousCount && m.Threshold <= newCount)
            .OrderBy(m => m.Threshold)
            .ToList();
    }
}

/// <summary>
/// Immutable descriptor for a single "total workouts" milestone.
/// </summary>
/// <param name="Threshold">How many lifetime workouts must be completed to earn this.</param>
/// <param name="Title">Short display name (matches the DB <c>ACHIEVEMENT.title</c> column).</param>
/// <param name="Description">One-sentence description shown in the rank showcase card.</param>
public sealed record WorkoutMilestone(int Threshold, string Title, string Description);

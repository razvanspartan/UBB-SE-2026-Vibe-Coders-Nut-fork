namespace VibeCoders.Domain;

public sealed class TotalWorkoutsMilestoneEvaluator
{
    public static IReadOnlyList<WorkoutMilestone> DefaultMilestones { get; } =
    [
        new WorkoutMilestone(Threshold: 1,   Title: "First Rep",       Description: "Complete your first workout."),
        new WorkoutMilestone(Threshold: 10,  Title: "Getting Serious", Description: "Complete 10 total workouts."),
        new WorkoutMilestone(Threshold: 25,  Title: "Gym Regular",     Description: "Complete 25 total workouts."),
        new WorkoutMilestone(Threshold: 50,  Title: "Iron Warrior",    Description: "Complete 50 total workouts."),
        new WorkoutMilestone(Threshold: 100, Title: "Gym Legend",      Description: "Complete 100 total workouts."),
    ];

    private readonly IReadOnlyList<WorkoutMilestone> _milestones;

    public TotalWorkoutsMilestoneEvaluator() : this(DefaultMilestones) { }

    public TotalWorkoutsMilestoneEvaluator(IReadOnlyList<WorkoutMilestone> milestones)
    {
        _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
    }

    public IReadOnlyList<WorkoutMilestone> GetEarnedMilestones(int lifetimeWorkoutCount)
    {
        if (lifetimeWorkoutCount < 0)
            throw new ArgumentOutOfRangeException(nameof(lifetimeWorkoutCount), "Count cannot be negative.");

        return _milestones
            .Where(m => lifetimeWorkoutCount >= m.Threshold)
            .OrderBy(m => m.Threshold)
            .ToList();
    }

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

public sealed record WorkoutMilestone(int Threshold, string Title, string Description);

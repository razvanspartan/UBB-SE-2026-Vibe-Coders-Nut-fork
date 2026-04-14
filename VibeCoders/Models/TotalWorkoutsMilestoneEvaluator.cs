namespace VibeCoders.Domain;

public sealed class TotalWorkoutsMilestoneEvaluator
{
    public static IReadOnlyList<WorkoutMilestone> DefaultMilestones { get; } =
    [
        new WorkoutMilestone(threshold: 1,   title: "First Rep",       description: "Complete your first workout."),
        new WorkoutMilestone(threshold: 10,  title: "Getting Serious", description: "Complete 10 total workouts."),
        new WorkoutMilestone(threshold: 25,  title: "Gym Regular",     description: "Complete 25 total workouts."),
        new WorkoutMilestone(threshold: 50,  title: "Iron Warrior",    description: "Complete 50 total workouts."),
        new WorkoutMilestone(threshold: 100, title: "Gym Legend",      description: "Complete 100 total workouts."),
    ];

    private readonly IReadOnlyList<WorkoutMilestone> milestones;

    public TotalWorkoutsMilestoneEvaluator()
        : this(DefaultMilestones)
    {
    }

    public TotalWorkoutsMilestoneEvaluator(IReadOnlyList<WorkoutMilestone> milestones)
    {
        this.milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
    }

    public IReadOnlyList<WorkoutMilestone> GetEarnedMilestones(int lifetimeWorkoutCount)
    {
        if (lifetimeWorkoutCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetimeWorkoutCount), "Count cannot be negative.");
        }

        return milestones
            .Where(m => lifetimeWorkoutCount >= m.threshold)
            .OrderBy(m => m.threshold)
            .ToList();
    }

    public IReadOnlyList<WorkoutMilestone> GetNewlyEarnedMilestones(int previousCount, int newCount)
    {
        if (previousCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previousCount));
        }

        if (newCount < previousCount)
        {
            throw new ArgumentOutOfRangeException(nameof(newCount), "New count cannot be less than previous count.");
        }

        return milestones
            .Where(m => m.threshold > previousCount && m.threshold <= newCount)
            .OrderBy(m => m.threshold)
            .ToList();
    }
}

public sealed record WorkoutMilestone(int threshold, string title, string description);

using VibeCoders.Models;

namespace VibeCoders.Tests.Mocks.DataFactories;

public static class WorkoutLogViewModelDataFactory
{
    public static LoggedExercise CreateLoggedExerciseWithAdjustmentNote()
    {
        return new LoggedExercise
        {
            ExerciseName = "Bench Press",
            IsSystemAdjusted = true,
            AdjustmentNote = "System adjusted after last workout.",
            Sets = new List<LoggedSet>
            {
                new LoggedSet { SetIndex = 2, ActualReps = 8, ActualWeight = 62.5 },
                new LoggedSet { SetIndex = 1, ActualReps = 10, ActualWeight = 60.0 },
            }
        };
    }

    public static LoggedExercise CreateLoggedExerciseWithoutAdjustmentNote()
    {
        return new LoggedExercise
        {
            ExerciseName = "Deadlift",
            PerformanceRatio = 0.85,
            Sets = new List<LoggedSet>
            {
                new LoggedSet { SetIndex = 4, ActualReps = 4, ActualWeight = 100.0 },
                new LoggedSet { SetIndex = 2, ActualReps = 6, ActualWeight = 90.0 },
            }
        };
    }
}
using VibeCoders.Models;

namespace VibeCoders.Tests.Mocks.DataFactories;

public static class WorkoutLogFactory
{
    public static WorkoutLog CreateEmptyWorkoutLog(int clientId = 1)
    {
        return new WorkoutLog
        {
            Id = 100,
            ClientId = clientId,
            WorkoutName = "Morning Stretch",
            Date = DateTime.Now,
            Duration = TimeSpan.FromMinutes(30),
            Type = WorkoutType.CUSTOM,
            Exercises = new List<LoggedExercise>()
        };
    }
    public static WorkoutLog CreateModerateIntensityWorkoutLog(int clientId = 1)
    {
        var exercises = new List<LoggedExercise>
        {
            new LoggedExercise
            {
                ExerciseName = "Jogging",
                MetabolicEquivalent = 5.0f, // Moderate threshold
                ExerciseCaloriesBurned = 200,
                TargetMuscles = MuscleGroup.LEGS
            },
            new LoggedExercise
            {
                ExerciseName = "Push-ups",
                MetabolicEquivalent = 6.0f, // Moderate threshold
                ExerciseCaloriesBurned = 150,
                TargetMuscles = MuscleGroup.CHEST
            }
        };
        var log = CreateEmptyWorkoutLog(clientId);
        log.WorkoutName = "Moderate Cardio and Strength";
        log.Exercises = exercises;
        log.Type = WorkoutType.CUSTOM;
        return log;
    }
    public static WorkoutLog CreateHighIntensityWorkoutLog(int clientId = 1)
    {
        var exercises = new List<LoggedExercise>
        {
            new LoggedExercise
            {
                ExerciseName = "Heavy Squats",
                MetabolicEquivalent = 8.5f, // Intense threshold
                ExerciseCaloriesBurned = 300,
                TargetMuscles = MuscleGroup.LEGS,
                Sets = new List<LoggedSet>
                {
                    new LoggedSet { SetNumber = 1, ActualReps = 10, ActualWeight = 100.0 }
                }
            },
            new LoggedExercise
            {
                ExerciseName = "Deadlift",
                MetabolicEquivalent = 9.0f, // Intense threshold
                ExerciseCaloriesBurned = 400,
                TargetMuscles = MuscleGroup.BACK
            }
        };

        var log = CreateEmptyWorkoutLog(clientId);
        log.WorkoutName = "High Intensity Strength Session";
        log.Exercises = exercises;
        log.Type = WorkoutType.PREBUILT;

        return log;
    }

    public static WorkoutLog CreateLowIntensityWorkoutLog(int clientId = 1)
    {
        var log = CreateEmptyWorkoutLog(clientId);
        log.WorkoutName = "Recovery Yoga";
        log.Exercises = new List<LoggedExercise>
        {
            new LoggedExercise
            {
                ExerciseName = "Yoga Flow",
                MetabolicEquivalent = 2.5f, // Light threshold
                ExerciseCaloriesBurned = 150,
                TargetMuscles = MuscleGroup.CORE
            }
        };

        return log;
    }

    public static WorkoutLog CreateSampleWorkoutLog(int clientId, int workoutTemplateId)
    {
        const int standardWorkoutDurationMinutes = 45;
        const int sampleWorkoutCaloriesBurned = 350;
        const int standardReps = 10;
        const int reducedReps = 8;
        const double standardWeightLbs = 100.0;
        const double perfectPerformanceRatio = 1.0;

        return new WorkoutLog
        {
            ClientId = clientId,
            WorkoutName = "Test Workout",
            Date = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(standardWorkoutDurationMinutes),
            SourceTemplateId = workoutTemplateId,
            Type = WorkoutType.CUSTOM,
            TotalCaloriesBurned = sampleWorkoutCaloriesBurned,
            IntensityTag = "moderate",
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Bench Press",
                    PerformanceRatio = perfectPerformanceRatio,
                    IsSystemAdjusted = false,
                    AdjustmentNote = string.Empty,
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = 0, ActualReps = standardReps, ActualWeight = standardWeightLbs, TargetReps = standardReps, TargetWeight = standardWeightLbs },
                        new LoggedSet { SetIndex = 1, ActualReps = standardReps, ActualWeight = standardWeightLbs, TargetReps = standardReps, TargetWeight = standardWeightLbs },
                        new LoggedSet { SetIndex = 2, ActualReps = reducedReps, ActualWeight = standardWeightLbs, TargetReps = standardReps, TargetWeight = standardWeightLbs },
                    }
                }
            }
        };
    }

    public static WorkoutLog CreateWorkoutLogWithMultipleSets(int clientId, int workoutTemplateId)
    {
        const int extendedWorkoutDurationMinutes = 60;
        const int veryHighCaloriesBurned = 500;
        const int standardReps = 10;
        const int slightlyReducedReps = 9;
        const int increasedReps = 12;
        const double standardWeightLbs = 100.0;
        const double heavyWeightLbs = 150.0;
        const double increasedWeightLbs = 140.0;
        const double perfectPerformanceRatio = 1.0;
        const double goodPerformanceRatio = 0.95;

        return new WorkoutLog
        {
            ClientId = clientId,
            WorkoutName = "Multi Exercise Workout",
            Date = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(extendedWorkoutDurationMinutes),
            SourceTemplateId = workoutTemplateId,
            Type = WorkoutType.CUSTOM,
            TotalCaloriesBurned = veryHighCaloriesBurned,
            IntensityTag = "high",
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Bench Press",
                    PerformanceRatio = goodPerformanceRatio,
                    IsSystemAdjusted = false,
                    AdjustmentNote = string.Empty,
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = 0, ActualReps = standardReps, ActualWeight = standardWeightLbs, TargetReps = standardReps, TargetWeight = standardWeightLbs },
                        new LoggedSet { SetIndex = 1, ActualReps = slightlyReducedReps, ActualWeight = standardWeightLbs, TargetReps = standardReps, TargetWeight = standardWeightLbs },
                    }
                },
                new LoggedExercise
                {
                    ExerciseName = "Squats",
                    PerformanceRatio = perfectPerformanceRatio,
                    IsSystemAdjusted = true,
                    AdjustmentNote = "Increased weight",
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = 0, ActualReps = increasedReps, ActualWeight = heavyWeightLbs, TargetReps = standardReps, TargetWeight = increasedWeightLbs },
                        new LoggedSet { SetIndex = 1, ActualReps = increasedReps, ActualWeight = heavyWeightLbs, TargetReps = standardReps, TargetWeight = increasedWeightLbs },
                        new LoggedSet { SetIndex = 2, ActualReps = standardReps, ActualWeight = heavyWeightLbs, TargetReps = standardReps, TargetWeight = increasedWeightLbs },
                    }
                }
            }
        };
    }
}
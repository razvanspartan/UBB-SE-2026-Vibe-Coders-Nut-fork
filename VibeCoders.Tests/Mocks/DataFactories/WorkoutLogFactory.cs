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
                Met = 5.0f, // Moderate threshold
                ExerciseCaloriesBurned = 200,
                TargetMuscles = MuscleGroup.LEGS
            },
            new LoggedExercise
            {
                ExerciseName = "Push-ups",
                Met = 6.0f, // Moderate threshold
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
                Met = 8.5f, // Intense threshold
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
                Met = 9.0f, // Intense threshold
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
                Met = 2.5f, // Light threshold
                ExerciseCaloriesBurned = 150,
                TargetMuscles = MuscleGroup.CORE
            }
        };

        return log;
    }
}
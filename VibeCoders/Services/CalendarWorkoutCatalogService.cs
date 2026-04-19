using VibeCoders.Models;

namespace VibeCoders.Services;

public sealed class CalendarWorkoutCatalogService : ICalendarWorkoutCatalogService
{
    private readonly IDataStorage dataStorage;

    public CalendarWorkoutCatalogService(IDataStorage dataStorage)
    {
        this.dataStorage = dataStorage;
    }

    public async Task<IReadOnlyList<WorkoutTemplate>> GetAvailableWorkoutsAsync(int clientId, TimeSpan timeout)
    {
        try
        {
            var dbLoadTask = Task.Run(() => dataStorage.GetAvailableWorkouts(clientId));
            var completedTask = await Task.WhenAny(dbLoadTask, Task.Delay(timeout));

            if (completedTask != dbLoadTask)
            {
                return GetFallbackWorkouts(clientId);
            }

            var workouts = await dbLoadTask;
            if (workouts == null || workouts.Count == 0)
            {
                return GetFallbackWorkouts(clientId);
            }

            return workouts;
        }
        catch
        {
            return GetFallbackWorkouts(clientId);
        }
    }

    public IReadOnlyList<WorkoutTemplate> GetFallbackWorkouts(int clientId)
    {
        var fullBodyStrength = new WorkoutTemplate
        {
            Id = -1,
            ClientId = clientId,
            Name = "Fallback - Full Body Strength",
            Type = WorkoutType.PREBUILT
        };
        fullBodyStrength.AddExercise(new TemplateExercise { Id = -101, WorkoutTemplateId = -1, Name = "Back Squat", MuscleGroup = MuscleGroup.LEGS, TargetSets = 4, TargetReps = 6 });
        fullBodyStrength.AddExercise(new TemplateExercise { Id = -102, WorkoutTemplateId = -1, Name = "Bench Press", MuscleGroup = MuscleGroup.CHEST, TargetSets = 4, TargetReps = 6 });
        fullBodyStrength.AddExercise(new TemplateExercise { Id = -103, WorkoutTemplateId = -1, Name = "Barbell Row", MuscleGroup = MuscleGroup.BACK, TargetSets = 4, TargetReps = 8 });

        var hiitConditioning = new WorkoutTemplate
        {
            Id = -2,
            ClientId = clientId,
            Name = "Fallback - HIIT Conditioning",
            Type = WorkoutType.PREBUILT
        };
        hiitConditioning.AddExercise(new TemplateExercise { Id = -201, WorkoutTemplateId = -2, Name = "Burpees", MuscleGroup = MuscleGroup.CORE, TargetSets = 4, TargetReps = 12 });
        hiitConditioning.AddExercise(new TemplateExercise { Id = -202, WorkoutTemplateId = -2, Name = "Jump Squats", MuscleGroup = MuscleGroup.LEGS, TargetSets = 4, TargetReps = 15 });
        hiitConditioning.AddExercise(new TemplateExercise { Id = -203, WorkoutTemplateId = -2, Name = "Mountain Climbers", MuscleGroup = MuscleGroup.CORE, TargetSets = 4, TargetReps = 20 });

        var pushPull = new WorkoutTemplate
        {
            Id = -3,
            ClientId = clientId,
            Name = "Fallback - Push Pull Split",
            Type = WorkoutType.PREBUILT
        };
        pushPull.AddExercise(new TemplateExercise { Id = -301, WorkoutTemplateId = -3, Name = "Overhead Press", MuscleGroup = MuscleGroup.SHOULDERS, TargetSets = 4, TargetReps = 8 });
        pushPull.AddExercise(new TemplateExercise { Id = -302, WorkoutTemplateId = -3, Name = "Pull-Ups", MuscleGroup = MuscleGroup.BACK, TargetSets = 4, TargetReps = 8 });
        pushPull.AddExercise(new TemplateExercise { Id = -303, WorkoutTemplateId = -3, Name = "Dumbbell Curl", MuscleGroup = MuscleGroup.ARMS, TargetSets = 3, TargetReps = 12 });

        var coreMobility = new WorkoutTemplate
        {
            Id = -4,
            ClientId = clientId,
            Name = "Fallback - Core and Mobility",
            Type = WorkoutType.PREBUILT
        };
        coreMobility.AddExercise(new TemplateExercise { Id = -401, WorkoutTemplateId = -4, Name = "Plank", MuscleGroup = MuscleGroup.CORE, TargetSets = 3, TargetReps = 60 });
        coreMobility.AddExercise(new TemplateExercise { Id = -402, WorkoutTemplateId = -4, Name = "Dead Bug", MuscleGroup = MuscleGroup.CORE, TargetSets = 3, TargetReps = 12 });
        coreMobility.AddExercise(new TemplateExercise { Id = -403, WorkoutTemplateId = -4, Name = "Hip Bridge", MuscleGroup = MuscleGroup.LEGS, TargetSets = 3, TargetReps = 15 });

        return new List<WorkoutTemplate>
        {
            fullBodyStrength,
            hiitConditioning,
            pushPull,
            coreMobility
        };
    }
}

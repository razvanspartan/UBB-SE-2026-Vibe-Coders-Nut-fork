namespace VibeCoders.Services.Interfaces
{
    using VibeCoders.Domain;
    using VibeCoders.Models;
    using VibeCoders.Models.Integration;

    public interface IClientService
    {
        List<WorkoutLog> GetWorkoutHistoryForClient(int clientId);

        bool UpdateWorkoutLog(WorkoutLog updatedWorkoutLog);

        bool FinalizeWorkout(WorkoutLog log);

        bool SaveSet(WorkoutLog log, string exerciseName, LoggedSet set);

        bool ModifyWorkout(WorkoutLog updatedLog);

        Task<bool> SyncNutritionAsync(
            NutritionSyncPayload payload,
            CancellationToken cancellationToken = default);

        NutritionSyncPayload BuildNutritionSyncPayload(int clientId);

        ClientService.ClientProfileSnapshot BuildClientProfileSnapshot(int clientId);

        NutritionPlan? GetActiveNutritionPlan(int clientId);

        string BuildEstimatedWorkoutDurationDisplay(IReadOnlyList<LoggedExercise> loggedExercises);

        WorkoutLog BuildUpdatedWorkoutLog(WorkoutLog sourceWorkoutLog, IReadOnlyList<LoggedExercise> updatedExercises);

        List<WorkoutTemplate> GetAvailableWorkoutsForClient(int clientId);

        List<WorkoutTemplate> GetCustomAndTrainerAssignedWorkoutsForClient(int clientId);

        WorkoutTemplate? FindWorkoutTemplateById(int clientId, int workoutTemplateId);

        Dictionary<string, double> GetPreviousBestWeights(int clientId);

        List<Achievement> GetAchievements(int clientId);

        List<Notification> GetNotifications(int clientId);

        void ConfirmDeload(Notification notification);
    }
}

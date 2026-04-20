namespace VibeCoders.Repositories.Interfaces
{
    using VibeCoders.Models;
    public interface IRepositoryWorkoutLog
    {
        double GetClientWeight(int clientId);

        bool SaveWorkoutLog(WorkoutLog log);

        List<WorkoutLog> GetWorkoutHistory(int clientId);

        bool UpdateWorkoutLog(WorkoutLog log);

        List<WorkoutLog> GetLastTwoLogsForExercise(int templateExerciseId);

        bool UpdateWorkoutLogFeedback(int workoutLogId, double rating, string notes);

        int GetTotalActiveTimeForClient(int clientId);
    }
}

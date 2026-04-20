using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Repositories.Interfaces
{
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

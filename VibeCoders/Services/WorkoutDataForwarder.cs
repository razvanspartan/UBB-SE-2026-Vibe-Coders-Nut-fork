using Microsoft.Data.Sqlite;
using VibeCoders.Models;

namespace VibeCoders.Services;

public sealed class WorkoutDataForwarder : IWorkoutDataForwarder
{
    private readonly IWorkoutAnalyticsStore _store;
    private readonly IAnalyticsDashboardRefreshBus _refreshBus;

    private const float LightThreshold = 3.0f;
    private const float ModerateThreshold = 6.0f;
    
    private const string LightIntensity = "light";
    private const string ModerateIntensity = "moderate";
    private const string IntenseIntensity = "intense";

    public WorkoutDataForwarder(
        IWorkoutAnalyticsStore store,
        IAnalyticsDashboardRefreshBus refreshBus)
    {
        _store = store;
        _refreshBus = refreshBus;
    }

    public async Task<int> ForwardCompletedWorkoutAsync(
        long userId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        log.TotalCaloriesBurned = log.Exercises.Sum(e => e.ExerciseCaloriesBurned);
        
        if (log.Exercises.Count > 0)
        {
            log.AverageMet = log.Exercises.Average(e => e.Met);
            log.IntensityTag = CalculateIntensityTag(log.AverageMet);
        }

        var logId = await _store.SaveWorkoutAsync(userId, log, cancellationToken);

        _refreshBus.RequestRefresh();

        return logId;
    }

    private static string CalculateIntensityTag(float averageMet)
    {
        if (averageMet < LightThreshold)
        {
            return LightIntensity;
        }
        else if (averageMet < ModerateThreshold)
        {
            return ModerateIntensity;
        }
        else
        {
            return IntenseIntensity;
        }
    }

    public int GetTotalActiveTimeForClient(int clientId)
    {
        string _connectionString = "";
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT SUM(Duration)
        FROM WorkoutLog
        WHERE ClientId = @ClientId
          AND IsFinalized = 1;
    ";
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        var result = cmd.ExecuteScalar();
        return result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }
}

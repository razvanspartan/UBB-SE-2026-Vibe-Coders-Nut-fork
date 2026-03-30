using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

/// <summary>
/// Analytics data access over the workout_log and workout_log_sets tables.
/// Implementations must use parameterized queries and scope every result
/// to the given user id.
/// </summary>
public interface IWorkoutAnalyticsStore
{
    /// <summary>Creates schema tables and indexes if they do not exist.</summary>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a completed workout session and its sets into the analytics
    /// tables. Returns the auto-generated workout_log id.
    /// </summary>
    Task<int> SaveWorkoutAsync(long userId, WorkoutLog log, CancellationToken cancellationToken = default);

    /// <summary>Returns aggregated KPIs for the dashboard summary cards.</summary>
    Task<DashboardSummary> GetDashboardSummaryAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Returns four ISO-week buckets covering the last four weeks.</summary>
    Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Returns one page of workout history, ordered by date descending.</summary>
    Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads set-level detail for one workout session.
    /// Returns null if the log does not exist or is not owned by the user.
    /// </summary>
    Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(long userId, int workoutLogId, CancellationToken cancellationToken = default);
}

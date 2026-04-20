using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Repositories.Interfaces;

public interface IWorkoutAnalyticsStore
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    Task<int> SaveWorkoutAsync(long clientId, WorkoutLog log, CancellationToken cancellationToken = default);

    Task<DashboardSummary> GetDashboardSummaryAsync(long clientId, CancellationToken cancellationToken = default);

    Task<TimeSpan> GetTotalActiveTimeAsync(long clientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(long clientId, CancellationToken cancellationToken = default);

    Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(long clientId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(long clientId, int workoutLogId, CancellationToken cancellationToken = default);
}

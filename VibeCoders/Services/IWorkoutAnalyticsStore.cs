using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

public interface IWorkoutAnalyticsStore
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    Task<int> SaveWorkoutAsync(long userId, WorkoutLog log, CancellationToken cancellationToken = default);

    Task<DashboardSummary> GetDashboardSummaryAsync(long userId, CancellationToken cancellationToken = default);

    Task<TimeSpan> GetTotalActiveTimeAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(long userId, CancellationToken cancellationToken = default);

    Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(long userId, int workoutLogId, CancellationToken cancellationToken = default);
}

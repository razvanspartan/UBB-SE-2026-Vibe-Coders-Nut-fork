using System.Globalization;
using Microsoft.Data.SqlClient;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

/// <summary>
/// SQL Server implementation of <see cref="IWorkoutAnalyticsStore"/>.
/// Reads from the shared schema tables created by <see cref="SqlDataStorage.EnsureSchemaCreated"/>.
/// Does NOT create its own tables — schema ownership belongs to SqlDataStorage.
/// All queries scope results to the given user_id via CLIENT join.
/// </summary>
public sealed class SqlWorkoutAnalyticsStore : IWorkoutAnalyticsStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqlWorkoutAnalyticsStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ── Schema ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            // Schema is managed by SqlDataStorage.EnsureSchemaCreated().
            // We only add analytics-specific indexes that don't conflict.
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_workout_log_client_date')
                CREATE INDEX ix_workout_log_client_date
                    ON WORKOUT_LOG (client_id, date DESC, workout_log_id DESC);", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_workout_log_sets_log_idx')
                CREATE INDEX ix_workout_log_sets_log_idx
                    ON WORKOUT_LOG_SETS (workout_log_id, sets);", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> SaveWorkoutAsync(
        long userId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();

        try
        {
            // Resolve client_id from user_id.
            int clientId;
            await using (var getClient = new SqlCommand(
                "SELECT client_id FROM CLIENT WHERE user_id = @uid;", conn, tx))
            {
                getClient.Parameters.AddWithValue("@uid", userId);
                var result = await getClient.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result == null)
                    throw new InvalidOperationException($"No client found for user_id {userId}.");
                clientId = Convert.ToInt32(result);
            }

            int logId;
            await using (var insertLog = new SqlCommand(@"
                INSERT INTO WORKOUT_LOG
                    (client_id, workout_id, date, total_duration, calories_burned, rating, intensity_tag)
                VALUES
                    (@clientId, @tmpl, @date, @dur, @cal, NULL, @intensity);
                SELECT SCOPE_IDENTITY();", conn, tx))
            {
                insertLog.Parameters.AddWithValue("@clientId", clientId);
                insertLog.Parameters.AddWithValue("@tmpl", log.SourceTemplateId);
                insertLog.Parameters.AddWithValue("@date", log.Date);
                insertLog.Parameters.AddWithValue("@dur", log.Duration.ToString());
                insertLog.Parameters.AddWithValue("@cal", log.TotalCaloriesBurned);
                insertLog.Parameters.AddWithValue("@intensity", log.IntensityTag ?? string.Empty);

                logId = Convert.ToInt32(
                    await insertLog.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                    CultureInfo.InvariantCulture);
                log.Id = logId;
            }

            foreach (var exercise in log.Exercises)
            {
                foreach (var set in exercise.Sets)
                {
                    await using var insertSet = new SqlCommand(@"
                        INSERT INTO WORKOUT_LOG_SETS
                            (workout_log_id, exercise_name, sets, reps, weight,
                             target_reps, target_weight, performance_ratio,
                             is_system_adjusted, adjustment_note)
                        VALUES
                            (@lid, @ex, @si, @ar, @aw, @tr, @tw, @ratio, @adjusted, @note);",
                        conn, tx);

                    insertSet.Parameters.AddWithValue("@lid", logId);
                    insertSet.Parameters.AddWithValue("@ex", exercise.ExerciseName);
                    insertSet.Parameters.AddWithValue("@si", set.SetIndex);
                    insertSet.Parameters.AddWithValue("@ar", (object?)set.ActualReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@aw", (object?)set.ActualWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tr", (object?)set.TargetReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tw", (object?)set.TargetWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@ratio", exercise.PerformanceRatio);
                    insertSet.Parameters.AddWithValue("@adjusted", exercise.IsSystemAdjusted);
                    insertSet.Parameters.AddWithValue("@note", exercise.AdjustmentNote);

                    await insertSet.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return logId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    // ── Dashboard Summary ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn, @"
            SELECT COUNT(*)
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var windowStart = today.AddDays(-6);

        var activeSeconds = await ScalarLongAsync(conn, @"
            SELECT ISNULL(SUM(
                CASE
                    WHEN ISDATE(wl.total_duration) = 1
                    THEN DATEDIFF(SECOND, '00:00:00', CAST(wl.total_duration AS TIME))
                    ELSE 0
                END), 0)
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid
              AND CAST(wl.date AS DATE) >= @start
              AND CAST(wl.date AS DATE) <= @end;",
            "@uid", userId,
            "@start", windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "@end", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);

        string? preferred = null;
        await using (var prefCmd = new SqlCommand(@"
            SELECT TOP 1 wt.name
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE c.user_id = @uid AND wt.name IS NOT NULL
            GROUP BY wt.name
            ORDER BY COUNT(*) DESC, wt.name ASC;", conn))
        {
            prefCmd.Parameters.AddWithValue("@uid", userId);
            await using var reader = await prefCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                preferred = reader.GetString(0);
        }

        return new DashboardSummary
        {
            TotalWorkouts = (int)Math.Min(int.MaxValue, total),
            TotalActiveTimeLastSevenDays = TimeSpan.FromSeconds(activeSeconds),
            PreferredWorkoutName = preferred
        };
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetTotalActiveTimeAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var totalSeconds = await ScalarLongAsync(conn, @"
            SELECT ISNULL(SUM(
                CASE
                    WHEN ISDATE(wl.total_duration) = 1
                    THEN DATEDIFF(SECOND, '00:00:00', CAST(wl.total_duration AS TIME))
                    ELSE 0
                END), 0)
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        return TimeSpan.FromSeconds(totalSeconds);
    }

    // ── Consistency ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = GetMondayOfWeek(today);
        var buckets = new List<ConsistencyWeekBucket>(4);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 4; i++)
        {
            var weekStart = mondayThisWeek.AddDays(-21 + i * 7);
            var weekEnd = weekStart.AddDays(7);

            var count = await ScalarLongAsync(conn, @"
                SELECT COUNT(*)
                FROM WORKOUT_LOG wl
                JOIN CLIENT c ON c.client_id = wl.client_id
                WHERE c.user_id = @uid
                  AND CAST(wl.date AS DATE) >= @start
                  AND CAST(wl.date AS DATE) <  @end;",
                "@uid", userId,
                "@start", weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "@end", weekEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cancellationToken).ConfigureAwait(false);

            buckets.Add(new ConsistencyWeekBucket
            {
                WeekStart = weekStart,
                WorkoutCount = (int)count
            });
        }

        return buckets;
    }

    // ── History Page ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(
        long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn, @"
            SELECT COUNT(*)
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand(@"
            SELECT
                wl.workout_log_id,
                ISNULL(wt.name, ''),
                wl.date,
                wl.total_duration,
                ISNULL(wl.calories_burned, 0),
                ISNULL(wl.intensity_tag, '')
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE c.user_id = @uid
            ORDER BY wl.date DESC, wl.workout_log_id DESC
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;", conn);

        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@skip", pageIndex * pageSize);
        cmd.Parameters.AddWithValue("@take", pageSize);

        var items = new List<WorkoutHistoryRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new WorkoutHistoryRow
            {
                Id = reader.GetInt32(0),
                WorkoutName = reader.GetString(1),
                LogDate = reader.GetDateTime(2),
                DurationSeconds = ParseDurationToSeconds(reader.IsDBNull(3) ? null : reader.GetString(3)),
                TotalCaloriesBurned = reader.GetInt32(4),
                IntensityTag = reader.GetString(5)
            });
        }

        return new WorkoutHistoryPageResult
        {
            TotalCount = (int)Math.Min(int.MaxValue, total),
            Items = items
        };
    }

    // ── Session Detail ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(
        long userId, int workoutLogId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        string? workoutName;
        DateTime logDate;
        int duration;
        int totalCalories;
        string intensityTag;

        await using (var head = new SqlCommand(@"
            SELECT
                ISNULL(wt.name, ''),
                wl.date,
                wl.total_duration,
                ISNULL(wl.calories_burned, 0),
                ISNULL(wl.intensity_tag, '')
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE wl.workout_log_id = @id AND c.user_id = @uid;", conn))
        {
            head.Parameters.AddWithValue("@id", workoutLogId);
            head.Parameters.AddWithValue("@uid", userId);

            await using var r = await head.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            workoutName = r.GetString(0);
            logDate = r.GetDateTime(1);
            duration = ParseDurationToSeconds(r.IsDBNull(2) ? null : r.GetString(2));
            totalCalories = r.GetInt32(3);
            intensityTag = r.GetString(4);
        }

        // Build per-exercise calorie info from sets (calories_burned per exercise
        // is not stored separately — we approximate from total calories proportionally).
        var exerciseCalories = new List<ExerciseCalorieInfo>();
        var sets = new List<WorkoutSetRow>();

        await using (var setsCmd = new SqlCommand(@"
            SELECT exercise_name, sets, reps, weight
            FROM WORKOUT_LOG_SETS
            WHERE workout_log_id = @lid
            ORDER BY exercise_name ASC, sets ASC;", conn))
        {
            setsCmd.Parameters.AddWithValue("@lid", workoutLogId);
            await using var sr = await setsCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var exerciseSetCounts = new Dictionary<string, int>();

            while (await sr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var exName = sr.GetString(0);
                sets.Add(new WorkoutSetRow
                {
                    ExerciseName = exName,
                    SetIndex = sr.GetInt32(1),
                    ActualReps = sr.IsDBNull(2) ? null : sr.GetInt32(2),
                    ActualWeight = sr.IsDBNull(3) ? null : sr.GetDouble(3)
                });

                exerciseSetCounts.TryGetValue(exName, out var count);
                exerciseSetCounts[exName] = count + 1;
            }

            int totalSets = exerciseSetCounts.Values.Sum();
            foreach (var (exName, setCount) in exerciseSetCounts)
            {
                int calories = totalSets > 0
                    ? (int)Math.Round((double)totalCalories * setCount / totalSets)
                    : 0;

                exerciseCalories.Add(new ExerciseCalorieInfo
                {
                    ExerciseName = exName,
                    CaloriesBurned = calories
                });
            }
        }

        return new WorkoutSessionDetail
        {
            WorkoutLogId = workoutLogId,
            WorkoutName = workoutName,
            LogDate = logDate,
            DurationSeconds = duration,
            TotalCaloriesBurned = totalCalories,
            IntensityTag = intensityTag,
            Sets = sets,
            ExerciseCalories = exerciseCalories
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static int ParseDurationToSeconds(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration)) return 0;
        if (TimeSpan.TryParse(duration, out var ts)) return (int)ts.TotalSeconds;
        return 0;
    }

    private static async Task<long> ScalarLongAsync(
        SqlConnection conn, string sql,
        string paramName, long paramValue,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(paramName, paramValue);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    private static async Task<long> ScalarLongAsync(
        SqlConnection conn, string sql,
        string p1, long v1, string p2, string v2, string p3, string v3,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(p1, v1);
        cmd.Parameters.AddWithValue(p2, v2);
        cmd.Parameters.AddWithValue(p3, v3);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    internal static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var dow = date.DayOfWeek;
        var offset = dow == DayOfWeek.Sunday ? 6 : (int)dow - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
    }
}
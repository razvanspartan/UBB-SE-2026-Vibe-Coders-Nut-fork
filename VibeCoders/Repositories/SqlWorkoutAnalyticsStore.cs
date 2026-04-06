using System.Globalization;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

public sealed class SqlWorkoutAnalyticsStore : IWorkoutAnalyticsStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqlWorkoutAnalyticsStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using (var cmd = new SqliteCommand(@"
                CREATE INDEX IF NOT EXISTS ix_workout_log_client_date
                    ON WORKOUT_LOG (client_id, date DESC, workout_log_id DESC);", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var cmd = new SqliteCommand(@"
                CREATE INDEX IF NOT EXISTS ix_workout_log_sets_log_idx
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

    public async Task<int> SaveWorkoutAsync(
        long clientId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

<<<<<<< HEAD
=======
        int cid = log.ClientId > 0 ? log.ClientId : (int)clientId;
        if (cid <= 0)
            throw new InvalidOperationException("Workout log must have a positive client id.");

>>>>>>> origin/main
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();

        try
        {
<<<<<<< HEAD
            int clientId;
            await using (var getClient = new SqliteCommand(
                "SELECT client_id FROM CLIENT WHERE user_id = @uid;", conn, tx))
            {
                getClient.Parameters.AddWithValue("@uid", userId);
                var result = await getClient.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result == null)
                    throw new InvalidOperationException($"No client found for user_id {userId}.");
                clientId = Convert.ToInt32(result);
            }

=======
>>>>>>> origin/main
            await using (var insertLog = new SqliteCommand(@"
                INSERT INTO WORKOUT_LOG
                    (client_id, workout_id, date, total_duration, calories_burned, rating, intensity_tag)
                VALUES
                    (@clientId, @tmpl, @date, @dur, @cal, NULL, @intensity);", conn, tx))
            {
<<<<<<< HEAD
                insertLog.Parameters.AddWithValue("@clientId",  clientId);
=======
                insertLog.Parameters.AddWithValue("@clientId",  cid);
>>>>>>> origin/main
                insertLog.Parameters.AddWithValue("@tmpl",      log.SourceTemplateId);
                insertLog.Parameters.AddWithValue("@date",      log.Date.ToString("o"));
                insertLog.Parameters.AddWithValue("@dur",       log.Duration.ToString());
                insertLog.Parameters.AddWithValue("@cal",       log.TotalCaloriesBurned);
                insertLog.Parameters.AddWithValue("@intensity", log.IntensityTag ?? string.Empty);
                await insertLog.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            int logId;
            await using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, tx))
            {
                logId = Convert.ToInt32(
                    await idCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                    CultureInfo.InvariantCulture);
                log.Id = logId;
            }

            foreach (var exercise in log.Exercises)
            {
                foreach (var set in exercise.Sets)
                {
                    await using var insertSet = new SqliteCommand(@"
                        INSERT INTO WORKOUT_LOG_SETS
                            (workout_log_id, exercise_name, sets, reps, weight,
                             target_reps, target_weight, performance_ratio,
                             is_system_adjusted, adjustment_note)
                        VALUES
                            (@lid, @ex, @si, @ar, @aw, @tr, @tw, @ratio, @adjusted, @note);",
                        conn, tx);

                    insertSet.Parameters.AddWithValue("@lid",      logId);
                    insertSet.Parameters.AddWithValue("@ex",       exercise.ExerciseName);
                    insertSet.Parameters.AddWithValue("@si",       set.SetIndex);
                    insertSet.Parameters.AddWithValue("@ar",       (object?)set.ActualReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@aw",       (object?)set.ActualWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tr",       (object?)set.TargetReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tw",       (object?)set.TargetWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@ratio",    exercise.PerformanceRatio);
                    insertSet.Parameters.AddWithValue("@adjusted", exercise.IsSystemAdjusted ? 1 : 0);
                    insertSet.Parameters.AddWithValue("@note",     exercise.AdjustmentNote);
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

    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        long clientId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn, @"
            SELECT COUNT(*)
            FROM WORKOUT_LOG wl
            WHERE wl.client_id = @cid;",
            "@cid", clientId, cancellationToken).ConfigureAwait(false);

        var today       = DateOnly.FromDateTime(DateTime.Today);
        var windowStart = today.AddDays(-6);

        var activeSeconds = await ScalarLongAsync(conn, @"
            SELECT COALESCE(SUM(
                CASE
                    WHEN wl.total_duration IS NOT NULL AND wl.total_duration != ''
                    THEN strftime('%s', wl.total_duration) - strftime('%s', '00:00:00')
                    ELSE 0
                END), 0)
            FROM WORKOUT_LOG wl
<<<<<<< HEAD
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid
              AND date(wl.date) >= @start
              AND date(wl.date) <= @end;",
            "@uid", userId,
=======
            WHERE wl.client_id = @cid
              AND date(wl.date) >= @start
              AND date(wl.date) <= @end;",
            "@cid", clientId,
>>>>>>> origin/main
            "@start", windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "@end",   today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);

        string? preferred = null;
        await using (var prefCmd = new SqliteCommand(@"
            SELECT wt.name
            FROM WORKOUT_LOG wl
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE wl.client_id = @cid AND wt.name IS NOT NULL
            GROUP BY wt.name
            ORDER BY COUNT(*) DESC, wt.name ASC
            LIMIT 1;", conn))
        {
            prefCmd.Parameters.AddWithValue("@cid", clientId);
            await using var reader = await prefCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                preferred = reader.GetString(0);
        }

        return new DashboardSummary
        {
            TotalWorkouts                = (int)Math.Min(int.MaxValue, total),
            TotalActiveTimeLastSevenDays = TimeSpan.FromSeconds(activeSeconds),
            PreferredWorkoutName         = preferred
        };
    }

    public async Task<TimeSpan> GetTotalActiveTimeAsync(
        long clientId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var totalSeconds = await ScalarLongAsync(conn, @"
            SELECT COALESCE(SUM(
                CASE
                    WHEN wl.total_duration IS NOT NULL AND wl.total_duration != ''
                    THEN strftime('%s', wl.total_duration) - strftime('%s', '00:00:00')
                    ELSE 0
                END), 0)
            FROM WORKOUT_LOG wl
            WHERE wl.client_id = @cid;",
            "@cid", clientId, cancellationToken).ConfigureAwait(false);

        return TimeSpan.FromSeconds(totalSeconds);
    }

    public async Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(
        long clientId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var today          = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = GetMondayOfWeek(today);
        var buckets        = new List<ConsistencyWeekBucket>(4);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 4; i++)
        {
            var weekStart = mondayThisWeek.AddDays(-21 + i * 7);
            var weekEnd   = weekStart.AddDays(7);

            var count = await ScalarLongAsync(conn, @"
                SELECT COUNT(*)
                FROM WORKOUT_LOG wl
<<<<<<< HEAD
                JOIN CLIENT c ON c.client_id = wl.client_id
                WHERE c.user_id = @uid
                  AND date(wl.date) >= @start
                  AND date(wl.date) <  @end;",
                "@uid",   userId,
=======
                WHERE wl.client_id = @cid
                  AND date(wl.date) >= @start
                  AND date(wl.date) <  @end;",
                "@cid",   clientId,
>>>>>>> origin/main
                "@start", weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "@end",   weekEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cancellationToken).ConfigureAwait(false);

            buckets.Add(new ConsistencyWeekBucket
            {
                WeekStart    = weekStart,
                WorkoutCount = (int)count
            });
        }

        return buckets;
    }

    public async Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(
        long clientId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0)  pageIndex = 0;
        if (pageSize  <= 0) pageSize  = 10;

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn, @"
            SELECT COUNT(*)
            FROM WORKOUT_LOG wl
            WHERE wl.client_id = @cid;",
            "@cid", clientId, cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqliteCommand(@"
            SELECT
                wl.workout_log_id,
                COALESCE(wt.name, ''),
                wl.date,
                wl.total_duration,
                COALESCE(wl.calories_burned, 0),
                COALESCE(wl.intensity_tag, '')
            FROM WORKOUT_LOG wl
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE wl.client_id = @cid
            ORDER BY wl.date DESC, wl.workout_log_id DESC
            LIMIT @take OFFSET @skip;", conn);

<<<<<<< HEAD
        cmd.Parameters.AddWithValue("@uid",  userId);
=======
        cmd.Parameters.AddWithValue("@cid",  clientId);
>>>>>>> origin/main
        cmd.Parameters.AddWithValue("@skip", pageIndex * pageSize);
        cmd.Parameters.AddWithValue("@take", pageSize);

        var items = new List<WorkoutHistoryRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new WorkoutHistoryRow
            {
                Id                  = reader.GetInt32(0),
                WorkoutName         = reader.GetString(1),
                LogDate             = DateTime.Parse(reader.GetString(2)),
                DurationSeconds     = ParseDurationToSeconds(reader.IsDBNull(3) ? null : reader.GetString(3)),
                TotalCaloriesBurned = reader.GetInt32(4),
                IntensityTag        = reader.GetString(5)
            });
        }

        return new WorkoutHistoryPageResult
        {
            TotalCount = (int)Math.Min(int.MaxValue, total),
            Items      = items
        };
    }

    public async Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(
        long clientId, int workoutLogId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        string? workoutName;
        DateTime logDate;
        int      duration;
        int      totalCalories;
        string   intensityTag;

        await using (var head = new SqliteCommand(@"
            SELECT
                COALESCE(wt.name, ''),
                wl.date,
                wl.total_duration,
                COALESCE(wl.calories_burned, 0),
                COALESCE(wl.intensity_tag, '')
            FROM WORKOUT_LOG wl
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE wl.workout_log_id = @id AND wl.client_id = @cid;", conn))
        {
            head.Parameters.AddWithValue("@id",  workoutLogId);
<<<<<<< HEAD
            head.Parameters.AddWithValue("@uid", userId);
=======
            head.Parameters.AddWithValue("@cid", clientId);
>>>>>>> origin/main

            await using var r = await head.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            workoutName   = r.GetString(0);
            logDate       = DateTime.Parse(r.GetString(1));
            duration      = ParseDurationToSeconds(r.IsDBNull(2) ? null : r.GetString(2));
            totalCalories = r.GetInt32(3);
            intensityTag  = r.GetString(4);
        }

        var exerciseCalories = new List<ExerciseCalorieInfo>();
        var sets             = new List<WorkoutSetRow>();

        await using (var setsCmd = new SqliteCommand(@"
            SELECT exercise_name, sets, reps, weight
            FROM WORKOUT_LOG_SETS
            WHERE workout_log_id = @lid
            ORDER BY rowid ASC;", conn))
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
                    SetIndex     = sr.GetInt32(1),
                    ActualReps   = sr.IsDBNull(2) ? null : sr.GetInt32(2),
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
                    ExerciseName    = exName,
                    CaloriesBurned  = calories
                });
            }
        }

        return new WorkoutSessionDetail
        {
            WorkoutLogId        = workoutLogId,
            WorkoutName         = workoutName,
            LogDate             = logDate,
            DurationSeconds     = duration,
            TotalCaloriesBurned = totalCalories,
            IntensityTag        = intensityTag,
            Sets                = sets,
            ExerciseCalories    = exerciseCalories
        };
    }

    private static int ParseDurationToSeconds(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration)) return 0;
        if (TimeSpan.TryParse(duration, out var ts)) return (int)ts.TotalSeconds;
        return 0;
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection conn, string sql,
        string paramName, long paramValue,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue(paramName, paramValue);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection conn, string sql,
        string p1, long v1, string p2, string v2, string p3, string v3,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue(p1, v1);
        cmd.Parameters.AddWithValue(p2, v2);
        cmd.Parameters.AddWithValue(p3, v3);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    internal static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var dow    = date.DayOfWeek;
        var offset = dow == DayOfWeek.Sunday ? 6 : (int)dow - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
    }
}
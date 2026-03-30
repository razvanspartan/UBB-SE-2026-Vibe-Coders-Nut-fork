using System.Globalization;
using Microsoft.Data.SqlClient;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

/// <summary>
/// SQL Server implementation of <see cref="IWorkoutAnalyticsStore"/>.
/// All queries use parameterized SQL and scope results to the given user id.
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

    // ?? Schema ???????????????????????????????????????????????????????????????

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            // workout_log table
            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='workout_log' AND xtype='U')
                CREATE TABLE workout_log (
                    id                    INT PRIMARY KEY IDENTITY(1,1),
                    user_id               BIGINT NOT NULL,
                    workout_name          VARCHAR(100) NOT NULL,
                    log_date              DATE NOT NULL,
                    duration_seconds      INT NOT NULL CHECK (duration_seconds >= 0),
                    source_template_id    INT NOT NULL DEFAULT 0,
                    total_calories_burned INT NOT NULL DEFAULT 0,
                    intensity_tag         VARCHAR(20) NOT NULL DEFAULT ''
                );", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // index on workout_log
            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_workout_log_user_date_id')
                CREATE INDEX ix_workout_log_user_date_id
                    ON workout_log (user_id, log_date DESC, id DESC);", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // workout_log_exercises table
            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='workout_log_exercises' AND xtype='U')
                CREATE TABLE workout_log_exercises (
                    id              INT PRIMARY KEY IDENTITY(1,1),
                    workout_log_id  INT NOT NULL,
                    exercise_name   VARCHAR(100) NOT NULL,
                    calories_burned INT NOT NULL DEFAULT 0,
                    FOREIGN KEY (workout_log_id)
                        REFERENCES workout_log(id) ON DELETE CASCADE
                );", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // index on workout_log_exercises
            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_workout_log_exercises_log')
                CREATE INDEX ix_workout_log_exercises_log
                    ON workout_log_exercises (workout_log_id);", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // workout_log_sets table
            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='workout_log_sets' AND xtype='U')
                CREATE TABLE workout_log_sets (
                    id              INT PRIMARY KEY IDENTITY(1,1),
                    workout_log_id  INT NOT NULL,
                    exercise_name   VARCHAR(100) NOT NULL,
                    set_index       INT NOT NULL,
                    target_reps     INT,
                    actual_reps     INT,
                    target_weight   FLOAT,
                    actual_weight   FLOAT,
                    FOREIGN KEY (workout_log_id)
                        REFERENCES workout_log(id) ON DELETE CASCADE
                );", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // index on workout_log_sets
            await using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='ix_workout_log_sets_log')
                CREATE INDEX ix_workout_log_sets_log
                    ON workout_log_sets (workout_log_id, set_index);", conn))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await AddColumnIfMissingAsync(conn,
                "workout_log", "total_calories_burned",
                "total_calories_burned INT NOT NULL DEFAULT 0",
                cancellationToken).ConfigureAwait(false);

            await AddColumnIfMissingAsync(conn,
                "workout_log", "intensity_tag",
                "intensity_tag VARCHAR(20) NOT NULL DEFAULT ''",
                cancellationToken).ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ?? Save ?????????????????????????????????????????????????????????????????

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
            int logId;
            await using (var insertLog = new SqlCommand(@"
                INSERT INTO workout_log
                    (user_id, workout_name, log_date, duration_seconds, source_template_id, total_calories_burned, intensity_tag)
                VALUES
                    (@uid, @name, @date, @dur, @tmpl, @cal, @intensity);
                SELECT SCOPE_IDENTITY();", conn, tx))
            {
                insertLog.Parameters.AddWithValue("@uid", userId);
                insertLog.Parameters.AddWithValue("@name", log.WorkoutName);
                insertLog.Parameters.AddWithValue("@date",
                    DateOnly.FromDateTime(log.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                insertLog.Parameters.AddWithValue("@dur", (int)log.Duration.TotalSeconds);
                insertLog.Parameters.AddWithValue("@tmpl", log.SourceTemplateId);
                insertLog.Parameters.AddWithValue("@cal", log.TotalCaloriesBurned);
                insertLog.Parameters.AddWithValue("@intensity", log.IntensityTag);

                logId = Convert.ToInt32(
                    await insertLog.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                    CultureInfo.InvariantCulture);
            }

            foreach (var exercise in log.Exercises)
            {
                await using (var insertExercise = new SqlCommand(@"
                    INSERT INTO workout_log_exercises (workout_log_id, exercise_name, calories_burned)
                    VALUES (@lid, @ex, @cal);", conn, tx))
                {
                    insertExercise.Parameters.AddWithValue("@lid", logId);
                    insertExercise.Parameters.AddWithValue("@ex", exercise.ExerciseName);
                    insertExercise.Parameters.AddWithValue("@cal", exercise.ExerciseCaloriesBurned);
                    await insertExercise.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var set in exercise.Sets)
                {
                    await using var insertSet = new SqlCommand(@"
                        INSERT INTO workout_log_sets
                            (workout_log_id, exercise_name, set_index,
                             target_reps, actual_reps, target_weight, actual_weight)
                        VALUES
                            (@lid, @ex, @si, @tr, @ar, @tw, @aw);", conn, tx);

                    insertSet.Parameters.AddWithValue("@lid", logId);
                    insertSet.Parameters.AddWithValue("@ex", exercise.ExerciseName);
                    insertSet.Parameters.AddWithValue("@si", set.SetIndex);
                    insertSet.Parameters.AddWithValue("@tr", (object?)set.TargetReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@ar", (object?)set.ActualReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tw", (object?)set.TargetWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@aw", (object?)set.ActualWeight ?? DBNull.Value);

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

    // ?? Dashboard Summary ????????????????????????????????????????????????????

    /// <inheritdoc />
    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn,
            "SELECT COUNT(*) FROM workout_log WHERE user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var windowStart = today.AddDays(-6);

        var activeSeconds = await ScalarLongAsync(conn, @"
            SELECT ISNULL(SUM(duration_seconds), 0)
            FROM workout_log
            WHERE user_id = @uid
              AND log_date >= @start
              AND log_date <= @end;",
            "@uid", userId,
            "@start", windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "@end", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);

        string? preferred = null;
        await using (var prefCmd = new SqlCommand(@"
            SELECT TOP 1 workout_name
            FROM workout_log
            WHERE user_id = @uid
            GROUP BY workout_name
            ORDER BY COUNT(*) DESC, workout_name ASC;", conn))
        {
            prefCmd.Parameters.AddWithValue("@uid", userId);
            await using var reader = await prefCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                preferred = reader.GetString(0);
            }
        }

        return new DashboardSummary
        {
            TotalWorkouts = (int)Math.Min(int.MaxValue, total),
            TotalActiveTimeLastSevenDays = TimeSpan.FromSeconds(activeSeconds),
            PreferredWorkoutName = preferred
        };
    }

    // ?? Consistency ??????????????????????????????????????????????????????????

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
                FROM workout_log
                WHERE user_id  = @uid
                  AND log_date >= @start
                  AND log_date <  @end;",
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

    // ?? History Page ?????????????????????????????????????????????????????????

    /// <inheritdoc />
    public async Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(
        long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn,
            "SELECT COUNT(*) FROM workout_log WHERE user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand(@"
            SELECT id, workout_name, log_date, duration_seconds, total_calories_burned, intensity_tag
            FROM workout_log
            WHERE user_id = @uid
            ORDER BY log_date DESC, id DESC
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
                DurationSeconds = reader.GetInt32(3),
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

    // ?? Session Detail ???????????????????????????????????????????????????????

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
            SELECT workout_name, log_date, duration_seconds, total_calories_burned, intensity_tag
            FROM workout_log
            WHERE id = @id AND user_id = @uid;", conn))
        {
            head.Parameters.AddWithValue("@id", workoutLogId);
            head.Parameters.AddWithValue("@uid", userId);

            await using var r = await head.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            workoutName = r.GetString(0);
            logDate = r.GetDateTime(1);
            duration = r.GetInt32(2);
            totalCalories = r.GetInt32(3);
            intensityTag = r.GetString(4);
        }

        var exerciseCalories = new List<ExerciseCalorieInfo>();
        await using (var exercisesCmd = new SqlCommand(@"
            SELECT exercise_name, calories_burned
            FROM workout_log_exercises
            WHERE workout_log_id = @lid
            ORDER BY exercise_name ASC;", conn))
        {
            exercisesCmd.Parameters.AddWithValue("@lid", workoutLogId);
            await using var er = await exercisesCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await er.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                exerciseCalories.Add(new ExerciseCalorieInfo
                {
                    ExerciseName = er.GetString(0),
                    CaloriesBurned = er.GetInt32(1)
                });
            }
        }

        var sets = new List<WorkoutSetRow>();
        await using (var setsCmd = new SqlCommand(@"
            SELECT exercise_name, set_index, actual_reps, actual_weight
            FROM workout_log_sets
            WHERE workout_log_id = @lid
            ORDER BY exercise_name ASC, set_index ASC;", conn))
        {
            setsCmd.Parameters.AddWithValue("@lid", workoutLogId);
            await using var sr = await setsCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await sr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                sets.Add(new WorkoutSetRow
                {
                    ExerciseName = sr.GetString(0),
                    SetIndex = sr.GetInt32(1),
                    ActualReps = sr.IsDBNull(2) ? null : sr.GetInt32(2),
                    ActualWeight = sr.IsDBNull(3) ? null : sr.GetDouble(3)
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

    // ?? Private helpers ??????????????????????????????????????????????????????

    private static async Task AddColumnIfMissingAsync(
        SqlConnection conn, string table, string column, string columnDef,
        CancellationToken ct)
    {
        await using var check = new SqlCommand(
            $"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'{table}') AND name = N'{column}';",
            conn);
        var count = Convert.ToInt64(
            await check.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0L,
            CultureInfo.InvariantCulture);
        if (count == 0)
        {
            await using var alter = new SqlCommand(
                $"ALTER TABLE {table} ADD {columnDef};", conn);
            await alter.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
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

    /// <summary>
    /// Returns the Monday (ISO 8601 week start) for the week containing the given date.
    /// </summary>
    internal static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var dow = date.DayOfWeek;
        var offset = dow == DayOfWeek.Sunday ? 6 : (int)dow - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
    }
}

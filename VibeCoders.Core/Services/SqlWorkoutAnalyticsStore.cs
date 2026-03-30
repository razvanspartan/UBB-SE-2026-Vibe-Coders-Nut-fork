using System.Globalization;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

/// <summary>
/// SQLite implementation of <see cref="IWorkoutAnalyticsStore"/>.
/// All queries use parameterized SQL and scope results to the given user id.
/// Dates are stored as ISO 8601 strings (yyyy-MM-dd) and compared as text.
/// </summary>
public sealed class SqlWorkoutAnalyticsStore : IWorkoutAnalyticsStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqlWorkoutAnalyticsStore(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    internal SqlWorkoutAnalyticsStore(string connectionString, bool raw)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await ExecutePragmaAsync(conn, cancellationToken).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS workout_log (
                    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id            INTEGER NOT NULL,
                    workout_name       TEXT    NOT NULL,
                    log_date           TEXT    NOT NULL,
                    duration_seconds   INTEGER NOT NULL CHECK (duration_seconds >= 0),
                    source_template_id INTEGER NOT NULL DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS ix_workout_log_user_date_id
                    ON workout_log (user_id, log_date DESC, id DESC);

                CREATE TABLE IF NOT EXISTS workout_log_sets (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    workout_log_id  INTEGER NOT NULL,
                    exercise_name   TEXT    NOT NULL,
                    set_index       INTEGER NOT NULL,
                    target_reps     INTEGER,
                    actual_reps     INTEGER,
                    target_weight   REAL,
                    actual_weight   REAL,
                    met             REAL,
                    FOREIGN KEY (workout_log_id)
                        REFERENCES workout_log(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ix_workout_log_sets_log
                    ON workout_log_sets (workout_log_id, set_index);
            ";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> SaveWorkoutAsync(
        long userId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await using var conn = OpenConnection();
        await using var tx = conn.BeginTransaction();

        await using var insertLog = conn.CreateCommand();
        insertLog.Transaction = tx;
        insertLog.CommandText = @"
            INSERT INTO workout_log (user_id, workout_name, log_date, duration_seconds, source_template_id)
            VALUES ($uid, $name, $date, $dur, $tmpl);
            SELECT last_insert_rowid();";
        insertLog.Parameters.AddWithValue("$uid", userId);
        insertLog.Parameters.AddWithValue("$name", log.WorkoutName);
        insertLog.Parameters.AddWithValue("$date",
            DateOnly.FromDateTime(log.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        insertLog.Parameters.AddWithValue("$dur", (int)log.Duration.TotalSeconds);
        insertLog.Parameters.AddWithValue("$tmpl", log.SourceTemplateId);

        var logId = Convert.ToInt32(
            await insertLog.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
            CultureInfo.InvariantCulture);

        foreach (var exercise in log.Exercises)
        {
            foreach (var set in exercise.Sets)
            {
                await using var insertSet = conn.CreateCommand();
                insertSet.Transaction = tx;
                insertSet.CommandText = @"
                    INSERT INTO workout_log_sets
                        (workout_log_id, exercise_name, set_index, target_reps, actual_reps, target_weight, actual_weight)
                    VALUES ($lid, $ex, $si, $tr, $ar, $tw, $aw);";
                insertSet.Parameters.AddWithValue("$lid", logId);
                insertSet.Parameters.AddWithValue("$ex", exercise.ExerciseName);
                insertSet.Parameters.AddWithValue("$si", set.SetIndex);
                insertSet.Parameters.AddWithValue("$tr", (object?)set.TargetReps ?? DBNull.Value);
                insertSet.Parameters.AddWithValue("$ar", (object?)set.ActualReps ?? DBNull.Value);
                insertSet.Parameters.AddWithValue("$tw", (object?)set.TargetWeight ?? DBNull.Value);
                insertSet.Parameters.AddWithValue("$aw", (object?)set.ActualWeight ?? DBNull.Value);
                await insertSet.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        tx.Commit();
        return logId;
    }

    /// <inheritdoc />
    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await using var conn = OpenConnection();

        var total = await ScalarLongAsync(conn,
            "SELECT COUNT(*) FROM workout_log WHERE user_id = $uid;",
            "$uid", userId, cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var windowStart = today.AddDays(-6);

        var activeSeconds = await ScalarLongAsync(conn,
            @"SELECT IFNULL(SUM(duration_seconds), 0)
              FROM workout_log
              WHERE user_id = $uid
                AND log_date >= $start
                AND log_date <= $end;",
            "$uid", userId,
            "$start", windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "$end", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);

        string? preferred = null;
        await using var prefCmd = conn.CreateCommand();
        prefCmd.CommandText = @"
            SELECT workout_name
            FROM workout_log
            WHERE user_id = $uid
            GROUP BY workout_name
            ORDER BY COUNT(*) DESC, workout_name ASC
            LIMIT 1;";
        prefCmd.Parameters.AddWithValue("$uid", userId);
        await using var reader = await prefCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
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
    public async Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = GetMondayOfWeek(today);
        var buckets = new List<ConsistencyWeekBucket>(4);
        await using var conn = OpenConnection();

        for (var i = 0; i < 4; i++)
        {
            var weekStart = mondayThisWeek.AddDays(-21 + i * 7);
            var weekEnd = weekStart.AddDays(7);

            var count = await ScalarLongAsync(conn,
                @"SELECT COUNT(*)
                  FROM workout_log
                  WHERE user_id = $uid
                    AND log_date >= $start
                    AND log_date < $end;",
                "$uid", userId,
                "$start", weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "$end", weekEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cancellationToken).ConfigureAwait(false);

            buckets.Add(new ConsistencyWeekBucket
            {
                WeekStart = weekStart,
                WorkoutCount = (int)count
            });
        }

        return buckets;
    }

    /// <inheritdoc />
    public async Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(
        long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await using var conn = OpenConnection();

        var total = await ScalarLongAsync(conn,
            "SELECT COUNT(*) FROM workout_log WHERE user_id = $uid;",
            "$uid", userId, cancellationToken).ConfigureAwait(false);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, workout_name, log_date, duration_seconds
            FROM workout_log
            WHERE user_id = $uid
            ORDER BY log_date DESC, id DESC
            LIMIT $take OFFSET $skip;";
        cmd.Parameters.AddWithValue("$uid", userId);
        cmd.Parameters.AddWithValue("$take", pageSize);
        cmd.Parameters.AddWithValue("$skip", pageIndex * pageSize);

        var items = new List<WorkoutHistoryRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new WorkoutHistoryRow
            {
                Id = reader.GetInt32(0),
                WorkoutName = reader.GetString(1),
                LogDate = DateOnly.Parse(reader.GetString(2), CultureInfo.InvariantCulture)
                    .ToDateTime(TimeOnly.MinValue),
                DurationSeconds = reader.GetInt32(3)
            });
        }

        return new WorkoutHistoryPageResult
        {
            TotalCount = (int)Math.Min(int.MaxValue, total),
            Items = items
        };
    }

    /// <inheritdoc />
    public async Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(
        long userId, int workoutLogId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await using var conn = OpenConnection();

        await using var head = conn.CreateCommand();
        head.CommandText = @"
            SELECT workout_name, log_date, duration_seconds
            FROM workout_log
            WHERE id = $id AND user_id = $uid;";
        head.Parameters.AddWithValue("$id", workoutLogId);
        head.Parameters.AddWithValue("$uid", userId);

        string? workoutName;
        string logDateStr;
        int duration;
        await using (var r = await head.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            workoutName = r.GetString(0);
            logDateStr = r.GetString(1);
            duration = r.GetInt32(2);
        }

        await using var setsCmd = conn.CreateCommand();
        setsCmd.CommandText = @"
            SELECT exercise_name, set_index, actual_reps, actual_weight
            FROM workout_log_sets
            WHERE workout_log_id = $lid
            ORDER BY exercise_name ASC, set_index ASC;";
        setsCmd.Parameters.AddWithValue("$lid", workoutLogId);

        var sets = new List<WorkoutSetRow>();
        await using (var sr = await setsCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
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

        var logDate = DateOnly.Parse(logDateStr, CultureInfo.InvariantCulture)
            .ToDateTime(TimeOnly.MinValue);

        return new WorkoutSessionDetail
        {
            WorkoutLogId = workoutLogId,
            WorkoutName = workoutName,
            LogDate = logDate,
            DurationSeconds = duration,
            Sets = sets
        };
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    private static async Task ExecutePragmaAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection conn, string sql, string paramName, long paramValue,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue(paramName, paramValue);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection conn, string sql,
        string p1, long v1, string p2, string v2, string p3, string v3,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue(p1, v1);
        cmd.Parameters.AddWithValue(p2, v2);
        cmd.Parameters.AddWithValue(p3, v3);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the Monday (ISO 8601 week start) for the week containing
    /// <paramref name="date"/> in the local calendar.
    /// </summary>
    internal static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var dow = date.DayOfWeek;
        var offset = dow == DayOfWeek.Sunday ? 6 : (int)dow - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
    }
}

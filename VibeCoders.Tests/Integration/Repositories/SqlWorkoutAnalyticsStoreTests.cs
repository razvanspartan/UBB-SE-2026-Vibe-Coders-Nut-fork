using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class SqlWorkoutAnalyticsStoreTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly SqlWorkoutAnalyticsStore store;

    public SqlWorkoutAnalyticsStoreTests()
    {
        this.connectionString = "Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared";
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        CreateSchema(this.connection);

        this.store = new SqlWorkoutAnalyticsStore(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        var schemaManager = new DatabaseSchemaManager("Data Source=:memory:");

        using var cmd = new SqliteCommand(
            @"
            CREATE TABLE IF NOT EXISTS CLIENT (
                client_id  INTEGER PRIMARY KEY,
                user_id    INTEGER NOT NULL,
                trainer_id INTEGER NOT NULL,
                weight     REAL,
                height     REAL
            );

            CREATE TABLE IF NOT EXISTS WORKOUT_TEMPLATE (
                workout_template_id INTEGER PRIMARY KEY,
                client_id           INTEGER NOT NULL,
                name                TEXT NOT NULL,
                type                TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS WORKOUT_LOG (
                workout_log_id  INTEGER PRIMARY KEY,
                client_id       INTEGER NOT NULL,
                workout_id      INTEGER,
                date            TEXT NOT NULL,
                total_duration  TEXT,
                type            TEXT NOT NULL DEFAULT 'CUSTOM',
                calories_burned INTEGER,
                rating          INTEGER,
                trainer_notes   TEXT,
                intensity_tag   TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS WORKOUT_LOG_SETS (
                workout_log_sets_id INTEGER PRIMARY KEY,
                workout_log_id      INTEGER NOT NULL,
                exercise_name       TEXT NOT NULL,
                sets                INTEGER NOT NULL,
                reps                INTEGER,
                weight              REAL,
                target_reps         INTEGER,
                target_weight       REAL,
                performance_ratio   REAL,
                is_system_adjusted  INTEGER NOT NULL DEFAULT 0,
                adjustment_note     TEXT
            );", connection);
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task EnsureCreatedAsync_ShouldCreateIndexes()
    {
        await this.store.EnsureCreatedAsync();

        using var cmd = new SqliteCommand(
            @"SELECT name FROM sqlite_master 
              WHERE type='index' AND name IN ('ix_workout_log_client_date', 'ix_workout_log_sets_log_idx');",
            this.connection);

        using var reader = await cmd.ExecuteReaderAsync();
        var indexes = new List<string>();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        indexes.Should().Contain("ix_workout_log_client_date");
        indexes.Should().Contain("ix_workout_log_sets_log_idx");
    }

    [Fact]
    public async Task EnsureCreatedAsync_ShouldBeIdempotent()
    {
        await this.store.EnsureCreatedAsync();
        await this.store.EnsureCreatedAsync();
        await this.store.EnsureCreatedAsync();

        using var cmd = new SqliteCommand(
            @"SELECT COUNT(*) FROM sqlite_master 
              WHERE type='index' AND name = 'ix_workout_log_client_date';",
            this.connection);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldInsertWorkoutLogAndReturnId()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: 1);

        var logId = await this.store.SaveWorkoutAsync(1, workoutLog);

        logId.Should().BeGreaterThan(0);
        workoutLog.Id.Should().Be(logId);

        using var cmd = new SqliteCommand(
            "SELECT client_id, workout_id, calories_burned, intensity_tag FROM WORKOUT_LOG WHERE workout_log_id = @id",
            this.connection);
        cmd.Parameters.AddWithValue("@id", logId);

        using var reader = await cmd.ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetInt32(0).Should().Be(1);
        reader.GetInt32(1).Should().Be(workoutLog.SourceTemplateId);
        reader.GetInt32(2).Should().Be(workoutLog.TotalCaloriesBurned);
        reader.GetString(3).Should().Be(workoutLog.IntensityTag);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldInsertAllSetsCorrectly()
    {
        var workoutLog = CreateWorkoutLogWithMultipleSets(clientId: 1);

        var logId = await this.store.SaveWorkoutAsync(1, workoutLog);

        using var cmd = new SqliteCommand(
            @"SELECT exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note
              FROM WORKOUT_LOG_SETS 
              WHERE workout_log_id = @id
              ORDER BY rowid ASC",
            this.connection);
        cmd.Parameters.AddWithValue("@id", logId);

        using var reader = await cmd.ExecuteReaderAsync();
        var sets = new List<(string ExerciseName, int SetIndex, int? Reps, double? Weight)>();

        while (await reader.ReadAsync())
        {
            sets.Add((
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetDouble(3)
            ));
        }

        sets.Should().HaveCount(5);
        sets[0].ExerciseName.Should().Be("Bench Press");
        sets[0].SetIndex.Should().Be(0);
        sets[0].Reps.Should().Be(10);
        sets[0].Weight.Should().Be(100);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldUseClientIdFromParameter_WhenLogClientIdIsZero()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: 0);

        var logId = await this.store.SaveWorkoutAsync(42, workoutLog);

        using var cmd = new SqliteCommand(
            "SELECT client_id FROM WORKOUT_LOG WHERE workout_log_id = @id",
            this.connection);
        cmd.Parameters.AddWithValue("@id", logId);

        var clientId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        clientId.Should().Be(42);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldUseClientIdFromLog_WhenLogClientIdIsPositive()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: 99);

        var logId = await this.store.SaveWorkoutAsync(42, workoutLog);

        using var cmd = new SqliteCommand(
            "SELECT client_id FROM WORKOUT_LOG WHERE workout_log_id = @id",
            this.connection);
        cmd.Parameters.AddWithValue("@id", logId);

        var clientId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        clientId.Should().Be(99);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldThrowException_WhenClientIdIsInvalid()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: 0);

        var act = () => this.store.SaveWorkoutAsync(0, workoutLog);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Workout log must have a positive client id.");
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldThrowArgumentNullException_WhenLogIsNull()
    {
        var act = () => this.store.SaveWorkoutAsync(1, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldRollbackTransaction_OnFailure()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: 1);
        workoutLog.Exercises.Clear();
        workoutLog.Exercises.Add(new LoggedExercise
        {
            ExerciseName = null!,
            Sets = new List<LoggedSet>
            {
                new LoggedSet { SetIndex = 0, ActualReps = 10 }
            }
        });

        var act = () => this.store.SaveWorkoutAsync(1, workoutLog);

        await act.Should().ThrowAsync<Exception>();

        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM WORKOUT_LOG WHERE client_id = 1", this.connection);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnCorrectTotalWorkouts()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Push Day");
        await SaveMultipleWorkouts(clientId: 1, count: 5);

        var summary = await this.store.GetDashboardSummaryAsync(1);

        summary.TotalWorkouts.Should().Be(5);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateTotalActiveTimeForLastSevenDays()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Full Body");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(1, 1, today.AddDays(-2), "00:45:00", 300, "moderate");
        InsertWorkoutLog(1, 1, today.AddDays(-5), "01:00:00", 400, "high");
        InsertWorkoutLog(1, 1, today.AddDays(-10), "01:30:00", 500, "high");

        var summary = await this.store.GetDashboardSummaryAsync(1);

        var expectedSeconds = (45 * 60) + (60 * 60);
        summary.TotalActiveTimeLastSevenDays.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnPreferredWorkoutName()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Push Day");
        InsertWorkoutTemplate(2, 1, "Pull Day");
        InsertWorkoutTemplate(3, 1, "Leg Day");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(1, 1, today.AddDays(-1), "00:45:00", 300, "moderate");
        InsertWorkoutLog(1, 1, today.AddDays(-2), "00:45:00", 300, "moderate");
        InsertWorkoutLog(1, 1, today.AddDays(-3), "00:45:00", 300, "moderate");
        InsertWorkoutLog(1, 2, today.AddDays(-4), "00:45:00", 300, "moderate");
        InsertWorkoutLog(1, 3, today.AddDays(-5), "00:45:00", 300, "moderate");

        var summary = await this.store.GetDashboardSummaryAsync(1);

        summary.PreferredWorkoutName.Should().Be("Push Day");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnNull_WhenNoWorkoutTemplatesExist()
    {
        InsertTestClient(1);

        var summary = await this.store.GetDashboardSummaryAsync(1);

        summary.PreferredWorkoutName.Should().BeNull();
    }

    [Fact]
    public async Task GetTotalActiveTimeAsync_ShouldCalculateCorrectTotalTime()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(1, 1, today, "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, today.AddDays(-1), "00:45:00", 300, "moderate");
        InsertWorkoutLog(1, 1, today.AddDays(-2), "01:15:00", 500, "high");

        var totalTime = await this.store.GetTotalActiveTimeAsync(1);

        var expectedSeconds = (30 * 60) + (45 * 60) + (75 * 60);
        totalTime.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task GetTotalActiveTimeAsync_ShouldReturnZero_WhenNoDurationSet()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(1, 1, today, null, 200, "light");

        var totalTime = await this.store.GetTotalActiveTimeAsync(1);

        totalTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetConsistencyLastFourWeeksAsync_ShouldReturnFourWeekBuckets()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Daily Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = SqlWorkoutAnalyticsStore.GetMondayOfWeek(today);

        InsertWorkoutLog(1, 1, mondayThisWeek.AddDays(-21), "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, mondayThisWeek.AddDays(-14), "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, mondayThisWeek.AddDays(-14).AddDays(1), "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, mondayThisWeek.AddDays(-7), "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, mondayThisWeek.AddDays(-7).AddDays(1), "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, mondayThisWeek.AddDays(-7).AddDays(2), "00:30:00", 200, "light");
        InsertWorkoutLog(1, 1, mondayThisWeek, "00:30:00", 200, "light");

        var buckets = await this.store.GetConsistencyLastFourWeeksAsync(1);

        buckets.Should().HaveCount(4);
        buckets[0].WorkoutCount.Should().Be(1);
        buckets[1].WorkoutCount.Should().Be(2);
        buckets[2].WorkoutCount.Should().Be(3);
        buckets[3].WorkoutCount.Should().Be(1);
    }

    [Fact]
    public async Task GetConsistencyLastFourWeeksAsync_ShouldReturnEmptyWeeks_WhenNoWorkouts()
    {
        InsertTestClient(1);

        var buckets = await this.store.GetConsistencyLastFourWeeksAsync(1);

        buckets.Should().HaveCount(4);
        buckets.Should().AllSatisfy(b => b.WorkoutCount.Should().Be(0));
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldReturnCorrectPageOfResults()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        for (int i = 0; i < 15; i++)
        {
            InsertWorkoutLog(1, 1, today.AddDays(-i), "00:30:00", 200 + i, "moderate");
        }

        var page = await this.store.GetWorkoutHistoryPageAsync(1, pageIndex: 1, pageSize: 5);

        page.TotalCount.Should().Be(15);
        page.Items.Should().HaveCount(5);
        page.Items[0].TotalCaloriesBurned.Should().Be(205);
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldHandleInvalidPageIndex()
    {
        InsertTestClient(1);

        var page = await this.store.GetWorkoutHistoryPageAsync(1, pageIndex: -5, pageSize: 10);

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldHandleInvalidPageSize()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(1, 1, today, "00:30:00", 200, "moderate");
        InsertWorkoutLog(1, 1, today.AddDays(-1), "00:30:00", 200, "moderate");

        var page = await this.store.GetWorkoutHistoryPageAsync(1, pageIndex: 0, pageSize: -10);

        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldOrderByDateDescending()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(1, 1, today.AddDays(-5), "00:30:00", 100, "light");
        InsertWorkoutLog(1, 1, today, "00:30:00", 300, "high");
        InsertWorkoutLog(1, 1, today.AddDays(-2), "00:30:00", 200, "moderate");

        var page = await this.store.GetWorkoutHistoryPageAsync(1, pageIndex: 0, pageSize: 10);

        page.Items[0].TotalCaloriesBurned.Should().Be(300);
        page.Items[1].TotalCaloriesBurned.Should().Be(200);
        page.Items[2].TotalCaloriesBurned.Should().Be(100);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldReturnFullDetails()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = InsertWorkoutLog(1, 1, today, "00:45:00", 400, "moderate");

        InsertWorkoutLogSet(logId, "Bench Press", 0, 10, 100, 10, 100, 1.0, false, null);
        InsertWorkoutLogSet(logId, "Bench Press", 1, 10, 100, 10, 100, 1.0, false, null);
        InsertWorkoutLogSet(logId, "Squats", 0, 12, 150, 12, 150, 1.0, false, null);

        var detail = await this.store.GetWorkoutSessionDetailAsync(1, logId);

        detail.Should().NotBeNull();
        detail!.WorkoutLogId.Should().Be(logId);
        detail.WorkoutName.Should().Be("Test Workout");
        detail.DurationSeconds.Should().Be(45 * 60);
        detail.TotalCaloriesBurned.Should().Be(400);
        detail.IntensityTag.Should().Be("moderate");
        detail.Sets.Should().HaveCount(3);
        detail.ExerciseCalories.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldCalculateExerciseCaloriesProportionally()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = InsertWorkoutLog(1, 1, today, "00:45:00", 300, "moderate");

        InsertWorkoutLogSet(logId, "Exercise A", 0, 10, 100, 10, 100, 1.0, false, null);
        InsertWorkoutLogSet(logId, "Exercise A", 1, 10, 100, 10, 100, 1.0, false, null);
        InsertWorkoutLogSet(logId, "Exercise B", 0, 10, 100, 10, 100, 1.0, false, null);

        var detail = await this.store.GetWorkoutSessionDetailAsync(1, logId);

        detail.Should().NotBeNull();
        detail!.ExerciseCalories.Should().HaveCount(2);

        var exerciseA = detail.ExerciseCalories.First(e => e.ExerciseName == "Exercise A");
        var exerciseB = detail.ExerciseCalories.First(e => e.ExerciseName == "Exercise B");

        exerciseA.CaloriesBurned.Should().Be(200);
        exerciseB.CaloriesBurned.Should().Be(100);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldReturnNull_WhenWorkoutNotFound()
    {
        InsertTestClient(1);

        var detail = await this.store.GetWorkoutSessionDetailAsync(1, 999);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldReturnNull_WhenClientIdDoesNotMatch()
    {
        InsertTestClient(1);
        InsertTestClient(2);
        InsertWorkoutTemplate(1, 1, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = InsertWorkoutLog(1, 1, today, "00:45:00", 400, "moderate");

        var detail = await this.store.GetWorkoutSessionDetailAsync(2, logId);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetMondayOfWeek_ShouldReturnMondayForVariousDays()
    {
        var monday = new DateOnly(2024, 1, 1);
        var tuesday = new DateOnly(2024, 1, 2);
        var wednesday = new DateOnly(2024, 1, 3);
        var sunday = new DateOnly(2024, 1, 7);

        SqlWorkoutAnalyticsStore.GetMondayOfWeek(monday).Should().Be(monday);
        SqlWorkoutAnalyticsStore.GetMondayOfWeek(tuesday).Should().Be(monday);
        SqlWorkoutAnalyticsStore.GetMondayOfWeek(wednesday).Should().Be(monday);
        SqlWorkoutAnalyticsStore.GetMondayOfWeek(sunday).Should().Be(monday);
    }

    [Fact]
    public async Task MultipleOperations_ShouldMaintainDataIntegrity()
    {
        InsertTestClient(1);
        InsertWorkoutTemplate(1, 1, "Complex Workout");

        var workoutLog1 = CreateWorkoutLogWithMultipleSets(clientId: 1);
        var workoutLog2 = CreateSampleWorkoutLog(clientId: 1);

        var logId1 = await this.store.SaveWorkoutAsync(1, workoutLog1);
        var logId2 = await this.store.SaveWorkoutAsync(1, workoutLog2);

        var summary = await this.store.GetDashboardSummaryAsync(1);
        var history = await this.store.GetWorkoutHistoryPageAsync(1, 0, 10);
        var detail1 = await this.store.GetWorkoutSessionDetailAsync(1, logId1);
        var detail2 = await this.store.GetWorkoutSessionDetailAsync(1, logId2);

        summary.TotalWorkouts.Should().Be(2);
        history.Items.Should().HaveCount(2);
        detail1.Should().NotBeNull();
        detail2.Should().NotBeNull();
        detail1!.Sets.Should().HaveCount(5);
        detail2!.Sets.Should().HaveCount(3);
    }

    private WorkoutLog CreateSampleWorkoutLog(int clientId)
    {
        return new WorkoutLog
        {
            ClientId = clientId,
            WorkoutName = "Test Workout",
            Date = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(45),
            SourceTemplateId = 1,
            Type = WorkoutType.CUSTOM,
            TotalCaloriesBurned = 350,
            IntensityTag = "moderate",
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Bench Press",
                    PerformanceRatio = 1.0,
                    IsSystemAdjusted = false,
                    AdjustmentNote = string.Empty,
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = 0, ActualReps = 10, ActualWeight = 100, TargetReps = 10, TargetWeight = 100 },
                        new LoggedSet { SetIndex = 1, ActualReps = 10, ActualWeight = 100, TargetReps = 10, TargetWeight = 100 },
                        new LoggedSet { SetIndex = 2, ActualReps = 8, ActualWeight = 100, TargetReps = 10, TargetWeight = 100 },
                    }
                }
            }
        };
    }

    private WorkoutLog CreateWorkoutLogWithMultipleSets(int clientId)
    {
        return new WorkoutLog
        {
            ClientId = clientId,
            WorkoutName = "Multi Exercise Workout",
            Date = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(60),
            SourceTemplateId = 1,
            Type = WorkoutType.CUSTOM,
            TotalCaloriesBurned = 500,
            IntensityTag = "high",
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Bench Press",
                    PerformanceRatio = 0.95,
                    IsSystemAdjusted = false,
                    AdjustmentNote = string.Empty,
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = 0, ActualReps = 10, ActualWeight = 100, TargetReps = 10, TargetWeight = 100 },
                        new LoggedSet { SetIndex = 1, ActualReps = 9, ActualWeight = 100, TargetReps = 10, TargetWeight = 100 },
                    }
                },
                new LoggedExercise
                {
                    ExerciseName = "Squats",
                    PerformanceRatio = 1.0,
                    IsSystemAdjusted = true,
                    AdjustmentNote = "Increased weight",
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = 0, ActualReps = 12, ActualWeight = 150, TargetReps = 10, TargetWeight = 140 },
                        new LoggedSet { SetIndex = 1, ActualReps = 12, ActualWeight = 150, TargetReps = 10, TargetWeight = 140 },
                        new LoggedSet { SetIndex = 2, ActualReps = 10, ActualWeight = 150, TargetReps = 10, TargetWeight = 140 },
                    }
                }
            }
        };
    }

    private async Task SaveMultipleWorkouts(long clientId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var log = CreateSampleWorkoutLog((int)clientId);
            log.Date = DateTime.UtcNow.AddDays(-i);
            await this.store.SaveWorkoutAsync(clientId, log);
        }
    }

    private void InsertTestClient(int clientId)
    {
        using var cmd = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@id, @uid, 1, 75.0, 180.0)",
            this.connection);
        cmd.Parameters.AddWithValue("@id", clientId);
        cmd.Parameters.AddWithValue("@uid", clientId + 1000);
        cmd.ExecuteNonQuery();
    }

    private void InsertWorkoutTemplate(int templateId, int clientId, string name)
    {
        using var cmd = new SqliteCommand(
            "INSERT INTO WORKOUT_TEMPLATE (workout_template_id, client_id, name, type) VALUES (@id, @cid, @name, 'CUSTOM')",
            this.connection);
        cmd.Parameters.AddWithValue("@id", templateId);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.ExecuteNonQuery();
    }

    private int InsertWorkoutLog(int clientId, int workoutId, DateOnly date, string? duration, int calories, string intensity)
    {
        using var cmd = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG (client_id, workout_id, date, total_duration, calories_burned, intensity_tag, type)
              VALUES (@cid, @wid, @date, @dur, @cal, @int, 'CUSTOM');
              SELECT last_insert_rowid();",
            this.connection);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@wid", workoutId);
        cmd.Parameters.AddWithValue("@date", date.ToString("o"));
        cmd.Parameters.AddWithValue("@dur", duration ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cal", calories);
        cmd.Parameters.AddWithValue("@int", intensity);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void InsertWorkoutLogSet(int logId, string exerciseName, int setIndex, int? reps, double? weight,
        int? targetReps, double? targetWeight, double performanceRatio, bool isSystemAdjusted, string? adjustmentNote)
    {
        using var cmd = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG_SETS 
              (workout_log_id, exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note)
              VALUES (@lid, @name, @set, @reps, @weight, @treps, @tweight, @ratio, @adjusted, @note)",
            this.connection);
        cmd.Parameters.AddWithValue("@lid", logId);
        cmd.Parameters.AddWithValue("@name", exerciseName);
        cmd.Parameters.AddWithValue("@set", setIndex);
        cmd.Parameters.AddWithValue("@reps", reps ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@weight", weight ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@treps", targetReps ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@tweight", targetWeight ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ratio", performanceRatio);
        cmd.Parameters.AddWithValue("@adjusted", isSystemAdjusted ? 1 : 0);
        cmd.Parameters.AddWithValue("@note", adjustmentNote ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}

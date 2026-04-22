using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class SqlWorkoutAnalyticsStoreTests : IDisposable
{
    private const int TestClientId = 1;
    private const int SecondTestClientId = 2;
    private const int ThirdTestClientId = 3;
    private const int TestWorkoutTemplateId = 1;
    private const int SecondTestWorkoutTemplateId = 2;
    private const int ThirdTestWorkoutTemplateId = 3;
    private const int InvalidClientId = 0;
    private const int NonExistentWorkoutId = 999;
    private const int UserIdOffset = 1000;
    private const int DefaultTrainerId = 1;

    private const double DefaultClientWeightKg = 75.0;
    private const double DefaultClientHeightCm = 180.0;

    private const int StandardWorkoutDurationMinutes = 45;
    private const int ExtendedWorkoutDurationMinutes = 60;
    private const int ShortWorkoutDurationMinutes = 30;
    private const int LongWorkoutDurationMinutes = 75;
    private const int VeryLongWorkoutDurationMinutes = 90;

    private const int LowCaloriesBurned = 100;
    private const int StandardCaloriesBurned = 200;
    private const int ModerateCaloriesBurned = 300;
    private const int SampleWorkoutCaloriesBurned = 350;
    private const int HighCaloriesBurned = 400;
    private const int VeryHighCaloriesBurned = 500;

    private const int StandardReps = 10;
    private const int ReducedReps = 8;
    private const int SlightlyReducedReps = 9;
    private const int IncreasedReps = 12;

    private const double StandardWeightLbs = 100.0;
    private const double HeavyWeightLbs = 150.0;
    private const double IncreasedWeightLbs = 140.0;

    private const double PerfectPerformanceRatio = 1.0;
    private const double GoodPerformanceRatio = 0.95;

    private const int FirstSetIndex = 0;
    private const int SecondSetIndex = 1;
    private const int ThirdSetIndex = 2;

    private const int SecondsPerMinute = 60;

    private const int ExpectedIndexCount = 1;
    private const int ExpectedNumberOfWeekBuckets = 4;

    private const int SmallPageSize = 5;
    private const int StandardPageSize = 10;
    private const int TotalHistoryRecords = 15;
    private const int FirstPageIndex = 0;
    private const int SecondPageIndex = 1;

    private const int TwoDaysAgo = -2;
    private const int FiveDaysAgo = -5;
    private const int TenDaysAgo = -10;
    private const int ThreeWeeksAgo = -21;
    private const int TwoWeeksAgo = -14;
    private const int OneWeekAgo = -7;
    private const int OneDayAgo = -1;
    private const int ThreeDaysAgo = -3;
    private const int FourDaysAgo = -4;

    private const int ExpectedTotalWorkoutsForMultipleOperation = 2;
    private const int ExpectedSetsForComplexWorkout = 5;
    private const int ExpectedSetsForSampleWorkout = 3;
    private const int ExpectedNumberOfExercises = 2;
    private const int ExpectedNumberOfSetsForThreeExercises = 3;

    private const int TotalWorkoutsForSummaryTest = 5;

    private const int ExerciseAExpectedCalories = 200;
    private const int ExerciseBExpectedCalories = 100;

    private const int ColumnIndexZero = 0;
    private const int ColumnIndexOne = 1;
    private const int ColumnIndexTwo = 2;
    private const int ColumnIndexThree = 3;

    private const int ExpectedWorkoutCountWeekZero = 1;
    private const int ExpectedWorkoutCountWeekOne = 2;
    private const int ExpectedWorkoutCountWeekTwo = 3;
    private const int ExpectedWorkoutCountWeekThree = 1;

    private const int ExpectedCaloriesAtPageIndexFive = 205;

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

        using var command = new SqliteCommand(
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
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task EnsureCreatedAsync_ShouldCreateIndexes()
    {
        await this.store.EnsureCreatedAsync();

        using var command = new SqliteCommand(
            @"SELECT name FROM sqlite_master 
              WHERE type='index' AND name IN ('ix_workout_log_client_date', 'ix_workout_log_sets_log_idx');",
            this.connection);

        using var reader = await command.ExecuteReaderAsync();
        var indexes = new List<string>();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(ColumnIndexZero));
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

        using var command = new SqliteCommand(
            @"SELECT COUNT(*) FROM sqlite_master 
              WHERE type='index' AND name = 'ix_workout_log_client_date';",
            this.connection);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        count.Should().Be(ExpectedIndexCount);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldInsertWorkoutLogAndReturnId()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: TestClientId);

        var logId = await this.store.SaveWorkoutAsync(TestClientId, workoutLog);

        logId.Should().BeGreaterThan(InvalidClientId);
        workoutLog.Id.Should().Be(logId);

        using var command = new SqliteCommand(
            "SELECT client_id, workout_id, calories_burned, intensity_tag FROM WORKOUT_LOG WHERE workout_log_id = @workoutLogId",
            this.connection);
        command.Parameters.AddWithValue("@workoutLogId", logId);

        using var reader = await command.ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetInt32(ColumnIndexZero).Should().Be(TestClientId);
        reader.GetInt32(ColumnIndexOne).Should().Be(workoutLog.SourceTemplateId);
        reader.GetInt32(ColumnIndexTwo).Should().Be(workoutLog.TotalCaloriesBurned);
        reader.GetString(ColumnIndexThree).Should().Be(workoutLog.IntensityTag);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldInsertAllSetsCorrectly()
    {
        var workoutLog = CreateWorkoutLogWithMultipleSets(clientId: TestClientId);

        var logId = await this.store.SaveWorkoutAsync(TestClientId, workoutLog);

        using var command = new SqliteCommand(
            @"SELECT exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note
              FROM WORKOUT_LOG_SETS 
              WHERE workout_log_id = @workoutLogId
              ORDER BY rowid ASC",
            this.connection);
        command.Parameters.AddWithValue("@workoutLogId", logId);

        using var reader = await command.ExecuteReaderAsync();
        var sets = new List<(string ExerciseName, int SetIndex, int? Reps, double? Weight)>();

        while (await reader.ReadAsync())
        {
            sets.Add((
                reader.GetString(ColumnIndexZero),
                reader.GetInt32(ColumnIndexOne),
                reader.IsDBNull(ColumnIndexTwo) ? null : reader.GetInt32(ColumnIndexTwo),
                reader.IsDBNull(ColumnIndexThree) ? null : reader.GetDouble(ColumnIndexThree)
            ));
        }

        sets.Should().HaveCount(ExpectedSetsForComplexWorkout);
        sets[FirstSetIndex].ExerciseName.Should().Be("Bench Press");
        sets[FirstSetIndex].SetIndex.Should().Be(FirstSetIndex);
        sets[FirstSetIndex].Reps.Should().Be(StandardReps);
        sets[FirstSetIndex].Weight.Should().Be(StandardWeightLbs);
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldThrowException_WhenClientIdIsInvalid()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: InvalidClientId);

        var act = () => this.store.SaveWorkoutAsync(InvalidClientId, workoutLog);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Workout log must have a positive client id.");
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldThrowArgumentNullException_WhenLogIsNull()
    {
        var act = () => this.store.SaveWorkoutAsync(TestClientId, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveWorkoutAsync_ShouldRollbackTransaction_OnFailure()
    {
        var workoutLog = CreateSampleWorkoutLog(clientId: TestClientId);
        workoutLog.Exercises.Clear();
        workoutLog.Exercises.Add(new LoggedExercise
        {
            ExerciseName = null!,
            Sets = new List<LoggedSet>
            {
                new LoggedSet { SetIndex = FirstSetIndex, ActualReps = StandardReps }
            }
        });

        var act = () => this.store.SaveWorkoutAsync(TestClientId, workoutLog);

        await act.Should().ThrowAsync<Exception>();

        using var countCommand = new SqliteCommand("SELECT COUNT(*) FROM WORKOUT_LOG WHERE client_id = @clientId", this.connection);
        countCommand.Parameters.AddWithValue("@clientId", TestClientId);
        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        count.Should().Be(InvalidClientId);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnCorrectTotalWorkouts()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Push Day");
        await SaveMultipleWorkouts(clientId: TestClientId, count: TotalWorkoutsForSummaryTest);

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        summary.TotalWorkouts.Should().Be(TotalWorkoutsForSummaryTest);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateTotalActiveTimeForLastSevenDays()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Full Body");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(FiveDaysAgo), "01:00:00", HighCaloriesBurned, "high");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TenDaysAgo), "01:30:00", VeryHighCaloriesBurned, "high");

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        var expectedSeconds = (StandardWorkoutDurationMinutes * SecondsPerMinute) + (ExtendedWorkoutDurationMinutes * SecondsPerMinute);
        summary.TotalActiveTimeLastSevenDays.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnPreferredWorkoutName()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Push Day");
        InsertWorkoutTemplate(SecondTestWorkoutTemplateId, TestClientId, "Pull Day");
        InsertWorkoutTemplate(ThirdTestWorkoutTemplateId, TestClientId, "Leg Day");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(OneDayAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(ThreeDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        InsertWorkoutLog(TestClientId, SecondTestWorkoutTemplateId, today.AddDays(FourDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        InsertWorkoutLog(TestClientId, ThirdTestWorkoutTemplateId, today.AddDays(FiveDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        summary.PreferredWorkoutName.Should().Be("Push Day");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnNull_WhenNoWorkoutTemplatesExist()
    {
        InsertTestClient(TestClientId);

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        summary.PreferredWorkoutName.Should().BeNull();
    }

    [Fact]
    public async Task GetTotalActiveTimeAsync_ShouldCalculateCorrectTotalTime()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(OneDayAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "01:15:00", VeryHighCaloriesBurned, "high");

        var totalTime = await this.store.GetTotalActiveTimeAsync(TestClientId);

        var expectedSeconds = (ShortWorkoutDurationMinutes * SecondsPerMinute) + (StandardWorkoutDurationMinutes * SecondsPerMinute) + (LongWorkoutDurationMinutes * SecondsPerMinute);
        totalTime.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task GetConsistencyLastFourWeeksAsync_ShouldReturnFourWeekBuckets()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Daily Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = SqlWorkoutAnalyticsStore.GetMondayOfWeek(today);

        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(ThreeWeeksAgo), "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(TwoWeeksAgo), "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(TwoWeeksAgo).AddDays(1), "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(OneWeekAgo), "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(OneWeekAgo).AddDays(1), "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(OneWeekAgo).AddDays(2), "00:30:00", StandardCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek, "00:30:00", StandardCaloriesBurned, "light");

        var buckets = await this.store.GetConsistencyLastFourWeeksAsync(TestClientId);

        buckets.Should().HaveCount(ExpectedNumberOfWeekBuckets);
        buckets[FirstSetIndex].WorkoutCount.Should().Be(ExpectedWorkoutCountWeekZero);
        buckets[SecondSetIndex].WorkoutCount.Should().Be(ExpectedWorkoutCountWeekOne);
        buckets[ThirdSetIndex].WorkoutCount.Should().Be(ExpectedWorkoutCountWeekTwo);
        buckets[3].WorkoutCount.Should().Be(ExpectedWorkoutCountWeekThree);
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldReturnCorrectPageOfResults()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        for (int iterationIndex = 0; iterationIndex < TotalHistoryRecords; iterationIndex++)
        {
            InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(-iterationIndex), "00:30:00", StandardCaloriesBurned + iterationIndex, "moderate");
        }

        var page = await this.store.GetWorkoutHistoryPageAsync(TestClientId, pageIndex: SecondPageIndex, pageSize: SmallPageSize);

        page.TotalCount.Should().Be(TotalHistoryRecords);
        page.Items.Should().HaveCount(SmallPageSize);
        page.Items[FirstSetIndex].TotalCaloriesBurned.Should().Be(ExpectedCaloriesAtPageIndexFive);
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldOrderByDateDescending()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(FiveDaysAgo), "00:30:00", LowCaloriesBurned, "light");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:30:00", ModerateCaloriesBurned, "high");
        InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "00:30:00", StandardCaloriesBurned, "moderate");

        var page = await this.store.GetWorkoutHistoryPageAsync(TestClientId, pageIndex: FirstPageIndex, pageSize: StandardPageSize);

        page.Items[FirstSetIndex].TotalCaloriesBurned.Should().Be(ModerateCaloriesBurned);
        page.Items[SecondSetIndex].TotalCaloriesBurned.Should().Be(StandardCaloriesBurned);
        page.Items[ThirdSetIndex].TotalCaloriesBurned.Should().Be(LowCaloriesBurned);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldReturnFullDetails()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:45:00", HighCaloriesBurned, "moderate");

        InsertWorkoutLogSet(logId, "Bench Press", FirstSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);
        InsertWorkoutLogSet(logId, "Bench Press", SecondSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);
        InsertWorkoutLogSet(logId, "Squats", FirstSetIndex, IncreasedReps, HeavyWeightLbs, IncreasedReps, HeavyWeightLbs, PerfectPerformanceRatio, false, null);

        var detail = await this.store.GetWorkoutSessionDetailAsync(TestClientId, logId);

        detail.Should().NotBeNull();
        detail!.WorkoutLogId.Should().Be(logId);
        detail.WorkoutName.Should().Be("Test Workout");
        detail.DurationSeconds.Should().Be(StandardWorkoutDurationMinutes * SecondsPerMinute);
        detail.TotalCaloriesBurned.Should().Be(HighCaloriesBurned);
        detail.IntensityTag.Should().Be("moderate");
        detail.Sets.Should().HaveCount(ExpectedNumberOfSetsForThreeExercises);
        detail.ExerciseCalories.Should().HaveCount(ExpectedNumberOfExercises);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldCalculateExerciseCaloriesProportionally()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:45:00", ModerateCaloriesBurned, "moderate");

        InsertWorkoutLogSet(logId, "Exercise A", FirstSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);
        InsertWorkoutLogSet(logId, "Exercise A", SecondSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);
        InsertWorkoutLogSet(logId, "Exercise B", FirstSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);

        var detail = await this.store.GetWorkoutSessionDetailAsync(TestClientId, logId);

        detail.Should().NotBeNull();
        detail!.ExerciseCalories.Should().HaveCount(ExpectedNumberOfExercises);

        var exerciseA = detail.ExerciseCalories.First(e => e.ExerciseName == "Exercise A");
        var exerciseB = detail.ExerciseCalories.First(e => e.ExerciseName == "Exercise B");

        exerciseA.CaloriesBurned.Should().Be(ExerciseAExpectedCalories);
        exerciseB.CaloriesBurned.Should().Be(ExerciseBExpectedCalories);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldReturnNull_WhenWorkoutNotFound()
    {
        InsertTestClient(TestClientId);

        var detail = await this.store.GetWorkoutSessionDetailAsync(TestClientId, NonExistentWorkoutId);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldReturnNull_WhenClientIdDoesNotMatch()
    {
        InsertTestClient(TestClientId);
        InsertTestClient(SecondTestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:45:00", HighCaloriesBurned, "moderate");

        var detail = await this.store.GetWorkoutSessionDetailAsync(SecondTestClientId, logId);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task MultipleOperations_ShouldMaintainDataIntegrity()
    {
        InsertTestClient(TestClientId);
        InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Complex Workout");

        var workoutLog1 = CreateWorkoutLogWithMultipleSets(clientId: TestClientId);
        var workoutLog2 = CreateSampleWorkoutLog(clientId: TestClientId);

        var logId1 = await this.store.SaveWorkoutAsync(TestClientId, workoutLog1);
        var logId2 = await this.store.SaveWorkoutAsync(TestClientId, workoutLog2);

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);
        var history = await this.store.GetWorkoutHistoryPageAsync(TestClientId, FirstPageIndex, StandardPageSize);
        var detail1 = await this.store.GetWorkoutSessionDetailAsync(TestClientId, logId1);
        var detail2 = await this.store.GetWorkoutSessionDetailAsync(TestClientId, logId2);

        summary.TotalWorkouts.Should().Be(ExpectedTotalWorkoutsForMultipleOperation);
        history.Items.Should().HaveCount(ExpectedTotalWorkoutsForMultipleOperation);
        detail1.Should().NotBeNull();
        detail2.Should().NotBeNull();
        detail1!.Sets.Should().HaveCount(ExpectedSetsForComplexWorkout);
        detail2!.Sets.Should().HaveCount(ExpectedSetsForSampleWorkout);
    }

    private WorkoutLog CreateSampleWorkoutLog(int clientId)
    {
        return new WorkoutLog
        {
            ClientId = clientId,
            WorkoutName = "Test Workout",
            Date = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(StandardWorkoutDurationMinutes),
            SourceTemplateId = TestWorkoutTemplateId,
            Type = WorkoutType.CUSTOM,
            TotalCaloriesBurned = SampleWorkoutCaloriesBurned,
            IntensityTag = "moderate",
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Bench Press",
                    PerformanceRatio = PerfectPerformanceRatio,
                    IsSystemAdjusted = false,
                    AdjustmentNote = string.Empty,
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = FirstSetIndex, ActualReps = StandardReps, ActualWeight = StandardWeightLbs, TargetReps = StandardReps, TargetWeight = StandardWeightLbs },
                        new LoggedSet { SetIndex = SecondSetIndex, ActualReps = StandardReps, ActualWeight = StandardWeightLbs, TargetReps = StandardReps, TargetWeight = StandardWeightLbs },
                        new LoggedSet { SetIndex = ThirdSetIndex, ActualReps = ReducedReps, ActualWeight = StandardWeightLbs, TargetReps = StandardReps, TargetWeight = StandardWeightLbs },
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
            Duration = TimeSpan.FromMinutes(ExtendedWorkoutDurationMinutes),
            SourceTemplateId = TestWorkoutTemplateId,
            Type = WorkoutType.CUSTOM,
            TotalCaloriesBurned = VeryHighCaloriesBurned,
            IntensityTag = "high",
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Bench Press",
                    PerformanceRatio = GoodPerformanceRatio,
                    IsSystemAdjusted = false,
                    AdjustmentNote = string.Empty,
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = FirstSetIndex, ActualReps = StandardReps, ActualWeight = StandardWeightLbs, TargetReps = StandardReps, TargetWeight = StandardWeightLbs },
                        new LoggedSet { SetIndex = SecondSetIndex, ActualReps = SlightlyReducedReps, ActualWeight = StandardWeightLbs, TargetReps = StandardReps, TargetWeight = StandardWeightLbs },
                    }
                },
                new LoggedExercise
                {
                    ExerciseName = "Squats",
                    PerformanceRatio = PerfectPerformanceRatio,
                    IsSystemAdjusted = true,
                    AdjustmentNote = "Increased weight",
                    Sets = new List<LoggedSet>
                    {
                        new LoggedSet { SetIndex = FirstSetIndex, ActualReps = IncreasedReps, ActualWeight = HeavyWeightLbs, TargetReps = StandardReps, TargetWeight = IncreasedWeightLbs },
                        new LoggedSet { SetIndex = SecondSetIndex, ActualReps = IncreasedReps, ActualWeight = HeavyWeightLbs, TargetReps = StandardReps, TargetWeight = IncreasedWeightLbs },
                        new LoggedSet { SetIndex = ThirdSetIndex, ActualReps = StandardReps, ActualWeight = HeavyWeightLbs, TargetReps = StandardReps, TargetWeight = IncreasedWeightLbs },
                    }
                }
            }
        };
    }

    private async Task SaveMultipleWorkouts(long clientId, int count)
    {
        for (int workoutIndex = 0; workoutIndex < count; workoutIndex++)
        {
            var log = CreateSampleWorkoutLog((int)clientId);
            log.Date = DateTime.UtcNow.AddDays(-workoutIndex);
            await this.store.SaveWorkoutAsync(clientId, log);
        }
    }

    private void InsertTestClient(int clientId)
    {
        using var command = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@clientId, @userId, @trainerId, @weight, @height)",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@userId", clientId + UserIdOffset);
        command.Parameters.AddWithValue("@trainerId", DefaultTrainerId);
        command.Parameters.AddWithValue("@weight", DefaultClientWeightKg);
        command.Parameters.AddWithValue("@height", DefaultClientHeightCm);
        command.ExecuteNonQuery();
    }

    private void InsertWorkoutTemplate(int templateId, int clientId, string name)
    {
        using var command = new SqliteCommand(
            "INSERT INTO WORKOUT_TEMPLATE (workout_template_id, client_id, name, type) VALUES (@templateId, @clientId, @name, 'CUSTOM')",
            this.connection);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@name", name);
        command.ExecuteNonQuery();
    }

    private int InsertWorkoutLog(int clientId, int workoutId, DateOnly date, string? duration, int calories, string intensity)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG (client_id, workout_id, date, total_duration, calories_burned, intensity_tag, type)
              VALUES (@clientId, @workoutId, @date, @duration, @calories, @intensity, 'CUSTOM');
              SELECT last_insert_rowid();",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@workoutId", workoutId);
        command.Parameters.AddWithValue("@date", date.ToString("o"));
        command.Parameters.AddWithValue("@duration", duration ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@calories", calories);
        command.Parameters.AddWithValue("@intensity", intensity);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void InsertWorkoutLogSet(int logId, string exerciseName, int setIndex, int? reps, double? weight,
        int? targetReps, double? targetWeight, double performanceRatio, bool isSystemAdjusted, string? adjustmentNote)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG_SETS 
              (workout_log_id, exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note)
              VALUES (@logId, @exerciseName, @setIndex, @reps, @weight, @targetReps, @targetWeight, @performanceRatio, @isSystemAdjusted, @adjustmentNote)",
            this.connection);
        command.Parameters.AddWithValue("@logId", logId);
        command.Parameters.AddWithValue("@exerciseName", exerciseName);
        command.Parameters.AddWithValue("@setIndex", setIndex);
        command.Parameters.AddWithValue("@reps", reps ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@weight", weight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@targetReps", targetReps ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@targetWeight", targetWeight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@performanceRatio", performanceRatio);
        command.Parameters.AddWithValue("@isSystemAdjusted", isSystemAdjusted ? 1 : 0);
        command.Parameters.AddWithValue("@adjustmentNote", adjustmentNote ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }
}

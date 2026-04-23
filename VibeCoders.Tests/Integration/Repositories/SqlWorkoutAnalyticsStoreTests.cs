using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
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

    private const int StandardWorkoutDurationMinutes = 45;
    private const int ExtendedWorkoutDurationMinutes = 60;
    private const int ShortWorkoutDurationMinutes = 30;
    private const int LongWorkoutDurationMinutes = 75;

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

    private const int TotalWorkoutsForSummaryTest = 5;

    private const int ExerciseAExpectedCalories = 200;
    private const int ExerciseBExpectedCalories = 100;

    private const int ExpectedWorkoutCountWeekZero = 1;
    private const int ExpectedWorkoutCountWeekOne = 2;
    private const int ExpectedWorkoutCountWeekTwo = 3;
    private const int ExpectedWorkoutCountWeekThree = 1;

    private const int ExpectedCaloriesForPageTwoFirstItem = 205;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly SqlWorkoutAnalyticsStore store;
    private readonly TestDataHelper dataHelper;

    public SqlWorkoutAnalyticsStoreTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);

        this.dataHelper = new TestDataHelper(this.connection);
        this.dataHelper.SetupTrainer();

        this.store = new SqlWorkoutAnalyticsStore(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
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
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Push Day");
        await SaveMultipleWorkouts(clientId: TestClientId, count: TotalWorkoutsForSummaryTest);

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        summary.TotalWorkouts.Should().Be(TotalWorkoutsForSummaryTest);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCalculateTotalActiveTimeForLastSevenDays()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Full Body");

        var today = DateOnly.FromDateTime(DateTime.Today);
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(FiveDaysAgo), "01:00:00", HighCaloriesBurned, "high");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TenDaysAgo), "01:30:00", VeryHighCaloriesBurned, "high");

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        var expectedSeconds = (StandardWorkoutDurationMinutes * SecondsPerMinute) + (ExtendedWorkoutDurationMinutes * SecondsPerMinute);
        summary.TotalActiveTimeLastSevenDays.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnPreferredWorkoutName()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Push Day");
        this.dataHelper.InsertWorkoutTemplate(SecondTestWorkoutTemplateId, TestClientId, "Pull Day");
        this.dataHelper.InsertWorkoutTemplate(ThirdTestWorkoutTemplateId, TestClientId, "Leg Day");

        var today = DateOnly.FromDateTime(DateTime.Today);
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(OneDayAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(ThreeDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        this.dataHelper.InsertWorkoutLog(TestClientId, SecondTestWorkoutTemplateId, today.AddDays(FourDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        this.dataHelper.InsertWorkoutLog(TestClientId, ThirdTestWorkoutTemplateId, today.AddDays(FiveDaysAgo), "00:45:00", ModerateCaloriesBurned, "moderate");

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        summary.PreferredWorkoutName.Should().Be("Push Day");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnNull_WhenNoWorkoutTemplatesExist()
    {
        this.dataHelper.InsertClient(TestClientId);

        var summary = await this.store.GetDashboardSummaryAsync(TestClientId);

        summary.PreferredWorkoutName.Should().BeNull();
    }

    [Fact]
    public async Task GetTotalActiveTimeAsync_ShouldCalculateCorrectTotalTime()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(OneDayAgo), "00:45:00", ModerateCaloriesBurned, "moderate");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "01:15:00", VeryHighCaloriesBurned, "high");

        var totalTime = await this.store.GetTotalActiveTimeAsync(TestClientId);

        var expectedSeconds = (ShortWorkoutDurationMinutes * SecondsPerMinute) + (StandardWorkoutDurationMinutes * SecondsPerMinute) + (LongWorkoutDurationMinutes * SecondsPerMinute);
        totalTime.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task GetConsistencyLastFourWeeksAsync_ShouldReturnFourWeekBuckets()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Daily Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = SqlWorkoutAnalyticsStore.GetMondayOfWeek(today);

        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(ThreeWeeksAgo), "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(TwoWeeksAgo), "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(TwoWeeksAgo).AddDays(1), "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(OneWeekAgo), "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(OneWeekAgo).AddDays(1), "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek.AddDays(OneWeekAgo).AddDays(2), "00:30:00", StandardCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, mondayThisWeek, "00:30:00", StandardCaloriesBurned, "light");

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
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        for (int iterationIndex = 0; iterationIndex < TotalHistoryRecords; iterationIndex++)
        {
            this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(-iterationIndex), "00:30:00", StandardCaloriesBurned + iterationIndex, "moderate");
        }

        var page = await this.store.GetWorkoutHistoryPageAsync(TestClientId, pageIndex: SecondPageIndex, pageSize: SmallPageSize);

        page.TotalCount.Should().Be(TotalHistoryRecords);
        page.Items.Should().HaveCount(SmallPageSize);
        page.Items[FirstSetIndex].TotalCaloriesBurned.Should().Be(ExpectedCaloriesForPageTwoFirstItem);
    }

    [Fact]
    public async Task GetWorkoutHistoryPageAsync_ShouldOrderByDateDescending()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Workout A");

        var today = DateOnly.FromDateTime(DateTime.Today);
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(FiveDaysAgo), "00:30:00", LowCaloriesBurned, "light");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:30:00", ModerateCaloriesBurned, "high");
        this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today.AddDays(TwoDaysAgo), "00:30:00", StandardCaloriesBurned, "moderate");

        var page = await this.store.GetWorkoutHistoryPageAsync(TestClientId, pageIndex: FirstPageIndex, pageSize: StandardPageSize);

        page.Items[FirstSetIndex].TotalCaloriesBurned.Should().Be(ModerateCaloriesBurned);
        page.Items[SecondSetIndex].TotalCaloriesBurned.Should().Be(StandardCaloriesBurned);
        page.Items[ThirdSetIndex].TotalCaloriesBurned.Should().Be(LowCaloriesBurned);
    }

    [Fact]
    public async Task GetWorkoutSessionDetailAsync_ShouldCalculateExerciseCaloriesProportionally()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Test Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logId = this.dataHelper.InsertWorkoutLog(TestClientId, TestWorkoutTemplateId, today, "00:45:00", ModerateCaloriesBurned, "moderate");

        this.dataHelper.InsertWorkoutLogSet(logId, "Exercise A", FirstSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);
        this.dataHelper.InsertWorkoutLogSet(logId, "Exercise A", SecondSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);
        this.dataHelper.InsertWorkoutLogSet(logId, "Exercise B", FirstSetIndex, StandardReps, StandardWeightLbs, StandardReps, StandardWeightLbs, PerfectPerformanceRatio, false, null);

        var detail = await this.store.GetWorkoutSessionDetailAsync(TestClientId, logId);

        detail.Should().NotBeNull();
        detail!.ExerciseCalories.Should().HaveCount(ExpectedNumberOfExercises);

        var exerciseA = detail.ExerciseCalories.First(e => e.ExerciseName == "Exercise A");
        var exerciseB = detail.ExerciseCalories.First(e => e.ExerciseName == "Exercise B");

        exerciseA.CaloriesBurned.Should().Be(ExerciseAExpectedCalories);
        exerciseB.CaloriesBurned.Should().Be(ExerciseBExpectedCalories);
    }

    [Fact]
    public async Task MultipleOperations_ShouldMaintainDataIntegrity()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Complex Workout");

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
}

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

    private const int StandardWorkoutDurationMinutes = 45;
    private const int ExtendedWorkoutDurationMinutes = 60;

    private const int StandardCaloriesBurned = 200;
    private const int ModerateCaloriesBurned = 300;
    private const int HighCaloriesBurned = 400;
    private const int VeryHighCaloriesBurned = 500;

    private const int StandardReps = 10;
    private const double StandardWeightLbs = 100.0;
    private const double PerfectPerformanceRatio = 1.0;

    private const int FirstSetIndex = 0;
    private const int SecondSetIndex = 1;
    private const int ThirdSetIndex = 2;

    private const int SecondsPerMinute = 60;
    private const int ExpectedNumberOfWeekBuckets = 4;

    private const int TwoDaysAgo = -2;
    private const int FiveDaysAgo = -5;
    private const int TenDaysAgo = -10;
    private const int ThreeWeeksAgo = -21;
    private const int TwoWeeksAgo = -14;
    private const int OneWeekAgo = -7;
    private const int OneDayAgo = -1;
    private const int ThreeDaysAgo = -3;
    private const int FourDaysAgo = -4;

    private const int ExpectedNumberOfExercises = 2;

    private const int ExerciseAExpectedCalories = 200;
    private const int ExerciseBExpectedCalories = 100;

    private const int ExpectedWorkoutCountWeekZero = 1;
    private const int ExpectedWorkoutCountWeekOne = 2;
    private const int ExpectedWorkoutCountWeekTwo = 3;
    private const int ExpectedWorkoutCountWeekThree = 1;

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
    public async Task GetConsistencyLastFourWeeksAsync_ShouldReturnFourWeekBuckets()
    {
        this.dataHelper.InsertClient(TestClientId);
        this.dataHelper.InsertWorkoutTemplate(TestWorkoutTemplateId, TestClientId, "Daily Workout");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = GetMondayOfWeek(today);

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

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        var offset = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
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
}

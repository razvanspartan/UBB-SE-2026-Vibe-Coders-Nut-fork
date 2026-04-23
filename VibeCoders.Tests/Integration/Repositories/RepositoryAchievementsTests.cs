using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryAchievementsTests : IDisposable
{
    private const int TestClientId = 1;
    private const int TestClientIdSecondary = 2;
    private const int TestAchievementIdFirst = 1;
    private const int TestAchievementIdSecond = 2;
    private const int TestAchievementIdThird = 3;
    private const int TestAchievementIdFourth = 4;

    private const int OneDayAgo = -1;
    private const int TwoDaysAgo = -2;
    private const int ThreeDaysAgo = -3;
    private const int FourDaysAgo = -4;
    private const int FiveDaysAgo = -5;
    private const int SixDaysAgo = -6;
    private const int SevenDaysAgo = -7;
    private const int EightDaysAgo = -8;
    private const int NineDaysAgo = -9;

    private const int ThresholdOneWorkout = 1;
    private const int ThresholdFiveWorkouts = 5;
    private const int ThresholdTenWorkouts = 10;
    private const int ThresholdFifteenWorkouts = 15;
    private const int ThresholdTwentyWorkouts = 20;

    private const int ThreeWorkouts = 3;
    private const int SevenWorkouts = 7;
    private const int TwelveWorkouts = 12;
    private const int FifteenWorkouts = 15;

    private const int ExpectedCountZero = 0;
    private const int ExpectedCountOne = 1;
    private const int ExpectedCountTwo = 2;
    private const int ExpectedCountThree = 3;
    private const int ExpectedCountFour = 4;
    private const int ExpectedCountFive = 5;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryAchievements repository;
    private readonly TestDataHelper testData;

    public RepositoryAchievementsTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);

        this.testData = new TestDataHelper(this.connection);
        this.testData.SetupTrainer();

        this.repository = new RepositoryAchievements(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    [Fact]
    public void GetWorkoutsInLastSevenDays_ShouldIncludeBoundaryDates_DaySixAndToday()
    {
        this.testData.InsertClient(TestClientId);
        var today = DateTime.UtcNow.Date;
        this.testData.InsertWorkoutLog(TestClientId, today);
        this.testData.InsertWorkoutLog(TestClientId, today.AddDays(SixDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, today.AddDays(SevenDaysAgo));

        var count = this.repository.GetWorkoutsInLastSevenDays(TestClientId);

        count.Should().Be(ExpectedCountTwo);
    }

    [Fact]
    public void GetWorkoutsInLastSevenDays_ShouldHandleMultipleWorkoutsOnBoundaryDay()
    {
        this.testData.InsertClient(TestClientId);
        var today = DateTime.UtcNow.Date;
        this.testData.InsertWorkoutLog(TestClientId, today.AddDays(SixDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, today.AddDays(SixDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, today.AddDays(SixDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, today.AddDays(SevenDaysAgo));

        var count = this.repository.GetWorkoutsInLastSevenDays(TestClientId);

        count.Should().Be(ExpectedCountThree);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnZero_WhenNoWorkouts()
    {
        this.testData.InsertClient(TestClientId);

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(TestClientId);

        streak.Should().Be(ExpectedCountZero);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnOne_ForSingleWorkout()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today);

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(TestClientId);

        streak.Should().Be(ExpectedCountOne);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnMaxStreak_WhenOldStreakLongerThanRecent()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(OneDayAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(FiveDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(SixDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(SevenDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(EightDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(NineDaysAgo));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(TestClientId);

        streak.Should().Be(ExpectedCountFive);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldIgnoreGapsGreaterThanOneDay()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(TwoDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(ThreeDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(FiveDaysAgo));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(TestClientId);

        streak.Should().Be(ExpectedCountTwo);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldHandleAlternatingStreakPatterns()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(TwoDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(ThreeDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(FourDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(SixDaysAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(SevenDaysAgo));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(TestClientId);

        streak.Should().Be(ExpectedCountThree);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldHandleMultipleWorkoutsOnSameDay()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today);
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(OneDayAgo));
        this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(TwoDaysAgo));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(TestClientId);

        streak.Should().Be(ExpectedCountThree);
    }

    [Fact]
    public void AwardAchievement_ShouldHandleIdempotency_MultipleAwardAttempts()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "First Workout", "Complete your first workout", "WORKOUT_COUNT", ThresholdOneWorkout);

        var firstResult = this.repository.AwardAchievement(TestClientId, TestAchievementIdFirst);
        var secondResult = this.repository.AwardAchievement(TestClientId, TestAchievementIdFirst);
        var thirdResult = this.repository.AwardAchievement(TestClientId, TestAchievementIdFirst);

        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
        thirdResult.Should().BeFalse();

        var count = this.testData.GetClientAchievementCount(TestClientId, TestAchievementIdFirst);
        count.Should().Be(ExpectedCountOne);
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldMaintainOrderingWithMultipleUnlockedAchievements()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "Achievement 1", "Description 1", "CRITERIA", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdSecond, "Achievement 2", "Description 2", "CRITERIA", ThresholdFiveWorkouts);
        this.testData.InsertAchievement(TestAchievementIdThird, "Achievement 3", "Description 3", "CRITERIA", ThresholdTenWorkouts);
        this.testData.InsertAchievement(TestAchievementIdFourth, "Achievement 4", "Description 4", "CRITERIA", ThresholdFifteenWorkouts);
        this.testData.InsertClientAchievement(TestClientId, TestAchievementIdThird, true);
        this.testData.InsertClientAchievement(TestClientId, TestAchievementIdFirst, true);

        var showcase = this.repository.GetAchievementShowcaseForClient(TestClientId);

        showcase.Should().HaveCount(ExpectedCountFour);
        showcase[0].AchievementId.Should().Be(TestAchievementIdFirst);
        showcase[0].IsUnlocked.Should().BeTrue();
        showcase[1].AchievementId.Should().Be(TestAchievementIdThird);
        showcase[1].IsUnlocked.Should().BeTrue();
        showcase[2].IsUnlocked.Should().BeFalse();
        showcase[3].IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldPreferUnlockedWhenFilteringDuplicateTitles()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "Same Title", "Description 1", "CRITERIA", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdSecond, "Same Title", "Description 2", "CRITERIA", ThresholdFiveWorkouts);
        this.testData.InsertClientAchievement(TestClientId, TestAchievementIdSecond, true);

        var showcase = this.repository.GetAchievementShowcaseForClient(TestClientId);

        showcase.Should().ContainSingle(a => a.Title == "Same Title");
        var sameTitle = showcase.First(a => a.Title == "Same Title");
        sameTitle.IsUnlocked.Should().BeTrue();
        sameTitle.AchievementId.Should().Be(TestAchievementIdSecond);
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldHandleCaseInsensitiveDuplicates()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "Achievement Title", "Description 1", "CRITERIA", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdSecond, "achievement title", "Description 2", "CRITERIA", ThresholdFiveWorkouts);
        this.testData.InsertAchievement(TestAchievementIdThird, "ACHIEVEMENT TITLE", "Description 3", "CRITERIA", ThresholdTenWorkouts);

        var showcase = this.repository.GetAchievementShowcaseForClient(TestClientId);

        showcase.Should().ContainSingle();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldUnlockOnExactThreshold()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "5 Workouts", "Complete 5 workouts", "WORKOUT_COUNT", ThresholdFiveWorkouts);

        for (int i = 0; i < ThresholdFiveWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);

        this.testData.GetClientAchievementUnlockedStatus(TestClientId, TestAchievementIdFirst).Should().BeTrue();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldUnlockMultipleMilestonesInOneCall()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "1 Workout", "Complete 1 workout", "WORKOUT_COUNT", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdSecond, "5 Workouts", "Complete 5 workouts", "WORKOUT_COUNT", ThresholdFiveWorkouts);
        this.testData.InsertAchievement(TestAchievementIdThird, "10 Workouts", "Complete 10 workouts", "WORKOUT_COUNT", ThresholdTenWorkouts);
        this.testData.InsertAchievement(TestAchievementIdFourth, "15 Workouts", "Complete 15 workouts", "WORKOUT_COUNT", ThresholdFifteenWorkouts);

        for (int i = 0; i < TwelveWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);

        this.testData.GetClientAchievementUnlockedStatus(TestClientId, TestAchievementIdFirst).Should().BeTrue();
        this.testData.GetClientAchievementUnlockedStatus(TestClientId, TestAchievementIdSecond).Should().BeTrue();
        this.testData.GetClientAchievementUnlockedStatus(TestClientId, TestAchievementIdThird).Should().BeTrue();
        this.testData.HasClientAchievement(TestClientId, TestAchievementIdFourth).Should().BeFalse();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldHandleMixedThresholdAndNullThreshold()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "Special", "No threshold", "CUSTOM", null);
        this.testData.InsertAchievement(TestAchievementIdSecond, "1 Workout", "Complete 1 workout", "WORKOUT_COUNT", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdThird, "Manual", "No threshold", "MANUAL", null);
        this.testData.InsertAchievement(TestAchievementIdFourth, "5 Workouts", "Complete 5 workouts", "WORKOUT_COUNT", ThresholdFiveWorkouts);

        for (int i = 0; i < ThresholdFiveWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);

        this.testData.HasClientAchievement(TestClientId, TestAchievementIdFirst).Should().BeFalse();
        this.testData.GetClientAchievementUnlockedStatus(TestClientId, TestAchievementIdSecond).Should().BeTrue();
        this.testData.HasClientAchievement(TestClientId, TestAchievementIdThird).Should().BeFalse();
        this.testData.GetClientAchievementUnlockedStatus(TestClientId, TestAchievementIdFourth).Should().BeTrue();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldBeIdempotentAcrossMultipleCalls()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "First Workout", "Complete 1 workout", "WORKOUT_COUNT", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdSecond, "5 Workouts", "Complete 5 workouts", "WORKOUT_COUNT", ThresholdFiveWorkouts);

        for (int i = 0; i < ThresholdFiveWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);
        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);
        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);

        this.testData.GetClientAchievementCount(TestClientId, TestAchievementIdFirst).Should().Be(ExpectedCountOne);
        this.testData.GetClientAchievementCount(TestClientId, TestAchievementIdSecond).Should().Be(ExpectedCountOne);
    }

    [Fact]
    public void ComplexScenario_ShouldHandleProgressiveAchievementUnlocking()
    {
        this.testData.InsertClient(TestClientId);
        this.testData.InsertAchievement(TestAchievementIdFirst, "Beginner", "Complete 1 workout", "WORKOUT_COUNT", ThresholdOneWorkout);
        this.testData.InsertAchievement(TestAchievementIdSecond, "Intermediate", "Complete 5 workouts", "WORKOUT_COUNT", ThresholdFiveWorkouts);
        this.testData.InsertAchievement(TestAchievementIdThird, "Advanced", "Complete 10 workouts", "WORKOUT_COUNT", ThresholdTenWorkouts);
        this.testData.InsertAchievement(TestAchievementIdFourth, "Expert", "Complete 20 workouts", "WORKOUT_COUNT", ThresholdTwentyWorkouts);

        for (int i = 0; i < ThreeWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);
        var showcaseAfterThree = this.repository.GetAchievementShowcaseForClient(TestClientId);
        showcaseAfterThree.Count(a => a.IsUnlocked).Should().Be(ExpectedCountOne);

        for (int i = ThreeWorkouts; i < SevenWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);
        var showcaseAfterSeven = this.repository.GetAchievementShowcaseForClient(TestClientId);
        showcaseAfterSeven.Count(a => a.IsUnlocked).Should().Be(ExpectedCountTwo);

        for (int i = SevenWorkouts; i < FifteenWorkouts; i++)
        {
            this.testData.InsertWorkoutLog(TestClientId, DateTime.Today.AddDays(-i));
        }

        this.repository.EvaluateAndUnlockWorkoutMilestones(TestClientId);
        var showcaseAfterFifteen = this.repository.GetAchievementShowcaseForClient(TestClientId);
        showcaseAfterFifteen.Count(a => a.IsUnlocked).Should().Be(ExpectedCountThree);
        showcaseAfterFifteen[0].IsUnlocked.Should().BeTrue();
        showcaseAfterFifteen[1].IsUnlocked.Should().BeTrue();
        showcaseAfterFifteen[2].IsUnlocked.Should().BeTrue();
        showcaseAfterFifteen[3].IsUnlocked.Should().BeFalse();
    }
}

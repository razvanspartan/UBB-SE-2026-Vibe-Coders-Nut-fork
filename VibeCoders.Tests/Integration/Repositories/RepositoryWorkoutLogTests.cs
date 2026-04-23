using Microsoft.Data.Sqlite;
using FluentAssertions;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryWorkoutLogTests : IDisposable
{
    private const int PrimaryClientId = 1;
    private const int SecondaryClientId = 2;
    private const int WorkoutTemplateIdentifier = 10;
    private const int MatchingTemplateExerciseIdentifier = 100;
    private const int NonMatchingTemplateExerciseIdentifier = 101;
    private const int OneDayAgo = -1;
    private const int TwoDaysAgo = -2;
    private const int ThreeDaysAgo = -3;
    private const string FifteenMinuteDuration = "00:15:00";
    private const string OneHourThirtyMinuteThirtySecondDuration = "01:30:30";
    private const string TenMinuteDuration = "00:10:00";
    private const string TwentyMinuteDuration = "00:20:00";
    private const string ThirtyMinuteDuration = "00:30:00";
    private const string FortyFiveMinuteDuration = "00:45:00";
    private const string EmptyDuration = "";
    private const string MatchingExerciseName = "Bench Press";
    private const string NonMatchingExerciseName = "Squat";
    private const string ChestMuscleGroup = "CHEST";
    private const string LegsMuscleGroup = "LEGS";
    private const int ExpectedPrimaryClientTotalActiveTimeSeconds = 6330;
    private const int ExpectedSingleWorkoutTotalActiveTimeSeconds = 600;
    private const int ExpectedReturnedLogCount = 2;
    private const int ExpectedExerciseCount = 1;
    private const int ExpectedSetCount = 1;

    private static readonly DateTime WorkoutDate = new(2026, 4, 23, 10, 0, 0);

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryWorkoutLog repositoryWorkoutLog;
    private readonly RepositoryWorkoutLogDataFactory repositoryWorkoutLogDataFactory;
    private readonly TestDataHelper testDataHelper;

    public RepositoryWorkoutLogTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);

        this.testDataHelper = new TestDataHelper(this.connection);
        this.repositoryWorkoutLogDataFactory = new RepositoryWorkoutLogDataFactory(this.connection);
        this.testDataHelper.SetupTrainer();

        this.repositoryWorkoutLog = new RepositoryWorkoutLog(this.connectionString);
    }

    public void Dispose()
    {
        this.connection.Dispose();
    }

    [Fact]
    public void GetTotalActiveTimeForClient_WhenClientHasMultipleTimedLogs_ReturnsSumInSecondsForThatClientOnly()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertClient(SecondaryClientId);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithDuration(PrimaryClientId, WorkoutDate, FifteenMinuteDuration);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithDuration(PrimaryClientId, WorkoutDate.AddDays(OneDayAgo), OneHourThirtyMinuteThirtySecondDuration);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithDuration(SecondaryClientId, WorkoutDate, TenMinuteDuration);

        var totalActiveTimeSeconds = this.repositoryWorkoutLog.GetTotalActiveTimeForClient(PrimaryClientId);

        totalActiveTimeSeconds.Should().Be(ExpectedPrimaryClientTotalActiveTimeSeconds);
    }

    [Fact]
    public void GetTotalActiveTimeForClient_WhenDurationIsNullOrEmpty_IgnoresMissingDurations()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithDuration(PrimaryClientId, WorkoutDate, TenMinuteDuration);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithDuration(PrimaryClientId, WorkoutDate.AddDays(OneDayAgo), null);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithDuration(PrimaryClientId, WorkoutDate.AddDays(TwoDaysAgo), EmptyDuration);

        var totalActiveTimeSeconds = this.repositoryWorkoutLog.GetTotalActiveTimeForClient(PrimaryClientId);

        totalActiveTimeSeconds.Should().Be(ExpectedSingleWorkoutTotalActiveTimeSeconds);
    }

    [Fact]
    public void GetLastTwoLogsForExercise_WhenMoreThanTwoMatchingLogsExist_ReturnsLatestTwoMatchingLogsInDescendingOrder()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutTemplate(WorkoutTemplateIdentifier, PrimaryClientId);
        this.repositoryWorkoutLogDataFactory.InsertTemplateExercise(MatchingTemplateExerciseIdentifier, WorkoutTemplateIdentifier, MatchingExerciseName, ChestMuscleGroup);
        this.repositoryWorkoutLogDataFactory.InsertTemplateExercise(NonMatchingTemplateExerciseIdentifier, WorkoutTemplateIdentifier, NonMatchingExerciseName, LegsMuscleGroup);

        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithExerciseSet(PrimaryClientId, WorkoutTemplateIdentifier, WorkoutDate.AddDays(ThreeDaysAgo), FifteenMinuteDuration, MatchingExerciseName);
        var secondMostRecentMatchingWorkoutLogIdentifier = this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithExerciseSet(
            PrimaryClientId,
            WorkoutTemplateIdentifier,
            WorkoutDate.AddDays(TwoDaysAgo),
            ThirtyMinuteDuration,
            MatchingExerciseName);
        var mostRecentMatchingWorkoutLogIdentifier = this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithExerciseSet(
            PrimaryClientId,
            WorkoutTemplateIdentifier,
            WorkoutDate.AddDays(OneDayAgo),
            FortyFiveMinuteDuration,
            MatchingExerciseName);
        this.repositoryWorkoutLogDataFactory.InsertWorkoutLogWithExerciseSet(PrimaryClientId, WorkoutTemplateIdentifier, WorkoutDate, TwentyMinuteDuration, NonMatchingExerciseName);

        var lastTwoLogsForExercise = this.repositoryWorkoutLog.GetLastTwoLogsForExercise(MatchingTemplateExerciseIdentifier);

        lastTwoLogsForExercise.Should().HaveCount(ExpectedReturnedLogCount);

        var mostRecentLog = lastTwoLogsForExercise[0];
        var secondMostRecentLog = lastTwoLogsForExercise[1];

        mostRecentLog.Id.Should().Be(mostRecentMatchingWorkoutLogIdentifier);
        mostRecentLog.ClientId.Should().Be(PrimaryClientId);
        mostRecentLog.Exercises.Should().HaveCount(ExpectedExerciseCount);
        mostRecentLog.Exercises[0].ExerciseName.Should().Be(MatchingExerciseName);
        mostRecentLog.Exercises[0].Sets.Should().HaveCount(ExpectedSetCount);

        secondMostRecentLog.Id.Should().Be(secondMostRecentMatchingWorkoutLogIdentifier);
        secondMostRecentLog.ClientId.Should().Be(PrimaryClientId);
        secondMostRecentLog.Exercises.Should().HaveCount(ExpectedExerciseCount);
        secondMostRecentLog.Exercises[0].ExerciseName.Should().Be(MatchingExerciseName);
        secondMostRecentLog.Exercises[0].Sets.Should().HaveCount(ExpectedSetCount);
    }
}
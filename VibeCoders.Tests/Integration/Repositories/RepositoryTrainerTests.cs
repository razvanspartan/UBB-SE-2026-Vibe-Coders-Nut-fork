using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryTrainerTests : IDisposable
{
    private const int TrainerIdentifier = 1;
    private const int PrimaryClientId = 1;
    private const int OneDayAgo = -1;
    private const string InitialWorkoutTemplateName = "Strength Day";
    private const string UpdatedWorkoutTemplateName = "Pull Day";
    private const string InitialExerciseName = "Bench Press";
    private const string UpdatedExerciseName = "Deadlift";
    private const int ExpectedReturnedClientCount = 1;
    private const int ExpectedWorkoutLogCount = 1;
    private const int ExpectedExerciseCount = 1;

    private static readonly DateTime WorkoutDate = new(2026, 4, 23, 10, 0, 0);

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryTrainer repositoryTrainer;
    private readonly RepositoryTrainerDataFactory repositoryTrainerDataFactory;
    private readonly TestDataHelper testDataHelper;

    public RepositoryTrainerTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);

        this.testDataHelper = new TestDataHelper(this.connection);
        this.repositoryTrainerDataFactory = new RepositoryTrainerDataFactory(this.connection);
        this.testDataHelper.SetupTrainer();

        this.repositoryTrainer = new RepositoryTrainer(this.connectionString);
    }

    public void Dispose()
    {
        this.connection.Dispose();
    }

    [Fact]
    public void GetTrainerClients_WhenClientHasMultipleWorkouts_ReturnsLatestWorkoutDateOnly()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.repositoryTrainerDataFactory.InsertWorkoutLog(PrimaryClientId, WorkoutDate.AddDays(OneDayAgo));
        this.repositoryTrainerDataFactory.InsertWorkoutLog(PrimaryClientId, WorkoutDate);

        var trainerClients = this.repositoryTrainer.GetTrainerClients(TrainerIdentifier);

        trainerClients.Should().HaveCount(ExpectedReturnedClientCount);
        trainerClients[0].WorkoutLog.Should().HaveCount(ExpectedWorkoutLogCount);
        trainerClients[0].WorkoutLog[0].Date.Should().Be(WorkoutDate);
    }

    [Fact]
    public void GetTrainerClients_WhenClientHasNoWorkoutHistory_ReturnsEmptyWorkoutLog()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);

        var trainerClients = this.repositoryTrainer.GetTrainerClients(TrainerIdentifier);

        trainerClients.Should().HaveCount(ExpectedReturnedClientCount);
        trainerClients[0].WorkoutLog.Should().BeEmpty();
    }

    [Fact]
    public void SaveTrainerWorkout_WhenTemplateAlreadyExists_ReplacesOldExercisesAndUpdatesTemplateName()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        var initialWorkoutTemplate = RepositoryTrainerDataFactory.CreateWorkoutTemplate(
            PrimaryClientId,
            InitialWorkoutTemplateName,
            InitialExerciseName,
            MuscleGroup.CHEST);
        var initialSaveResult = this.repositoryTrainer.SaveTrainerWorkout(initialWorkoutTemplate);
        var updatedWorkoutTemplate = RepositoryTrainerDataFactory.CreateWorkoutTemplate(
            PrimaryClientId,
            UpdatedWorkoutTemplateName,
            UpdatedExerciseName,
            MuscleGroup.BACK);
        updatedWorkoutTemplate.Id = initialWorkoutTemplate.Id;

        var updateResult = this.repositoryTrainer.SaveTrainerWorkout(updatedWorkoutTemplate);
        var savedWorkoutTemplateName = this.repositoryTrainerDataFactory.GetWorkoutTemplateName(updatedWorkoutTemplate.Id);
        var savedExerciseNames = this.repositoryTrainerDataFactory.GetTemplateExerciseNames(updatedWorkoutTemplate.Id);

        initialSaveResult.Should().BeTrue();
        updateResult.Should().BeTrue();
        savedWorkoutTemplateName.Should().Be(UpdatedWorkoutTemplateName);
        savedExerciseNames.Should().HaveCount(ExpectedExerciseCount);
        savedExerciseNames[0].Should().Be(UpdatedExerciseName);
    }
}
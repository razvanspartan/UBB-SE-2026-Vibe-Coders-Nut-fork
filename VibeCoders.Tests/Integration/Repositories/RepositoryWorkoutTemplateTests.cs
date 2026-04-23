using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryWorkoutTemplateTests : IDisposable
{
    private const int PrimaryClientId = 1;
    private const int SecondaryClientId = 2;
    private const int PrebuiltTemplateId = 10;
    private const int TrainerAssignedTemplateId = 20;
    private const int CustomTemplateId = 30;
    private const int DifferentClientCustomTemplateId = 40;
    private const int FirstExerciseId = 100;
    private const int SecondExerciseId = 101;
    private const int ThirdExerciseId = 102;

    private const int ExpectedOneTemplate = 1;
    private const int ExpectedTwoTemplates = 2;
    private const int ExpectedThreeTemplates = 3;
    private const int ExpectedTwoExercises = 2;

    private const int DefaultTargetSets = 3;
    private const int DefaultTargetReps = 10;
    private const double DefaultTargetWeight = 50.0;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryWorkoutTemplate repository;
    private readonly TestDataHelper testDataHelper;

    public RepositoryWorkoutTemplateTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);

        this.testDataHelper = new TestDataHelper(this.connection);
        this.testDataHelper.SetupTrainer();

        this.repository = new RepositoryWorkoutTemplate(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldHandleVariousTypeFormatting_WithUnderscoresAndHyphens()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Prebuilt Workout", "PRE_BUILT");
        this.testDataHelper.InsertWorkoutTemplate(TrainerAssignedTemplateId, PrimaryClientId, "Assigned Workout", "TRAINER-ASSIGNED");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedTwoTemplates);
        availableWorkouts.Should().Contain(w => w.Type == WorkoutType.PREBUILT);
        availableWorkouts.Should().Contain(w => w.Type == WorkoutType.TRAINER_ASSIGNED);
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldReturnAllEligibleTemplates_WithCorrectFiltering()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertClient(SecondaryClientId);
        this.testDataHelper.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Universal Strength", "PREBUILT");
        this.testDataHelper.InsertWorkoutTemplate(TrainerAssignedTemplateId, PrimaryClientId, "Custom Plan", "TRAINERASSIGNED");
        this.testDataHelper.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "Personal Routine", "CUSTOM");
        this.testDataHelper.InsertWorkoutTemplate(DifferentClientCustomTemplateId, SecondaryClientId, "Another Custom", "CUSTOM");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedThreeTemplates);
        availableWorkouts.Should().Contain(w => w.Type == WorkoutType.PREBUILT);
        availableWorkouts.Should().Contain(w => w.Type == WorkoutType.TRAINER_ASSIGNED);
        availableWorkouts.Should().Contain(w => w.Type == WorkoutType.CUSTOM);
        availableWorkouts.All(w => w.ClientId == PrimaryClientId || w.Type == WorkoutType.PREBUILT).Should().BeTrue();
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldLoadExercisesForEachTemplate()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Full Body", "PREBUILT");
        this.testDataHelper.InsertTemplateExercise(FirstExerciseId, PrebuiltTemplateId, "Bench Press", "CHEST", DefaultTargetSets, DefaultTargetReps, DefaultTargetWeight);
        this.testDataHelper.InsertTemplateExercise(SecondExerciseId, PrebuiltTemplateId, "Squat", "LEGS", DefaultTargetSets, DefaultTargetReps, DefaultTargetWeight);

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        availableWorkouts[0].GetExercises().Should().HaveCount(ExpectedTwoExercises);
        availableWorkouts[0].GetExercises().Should().Contain(e => e.Name == "Bench Press");
        availableWorkouts[0].GetExercises().Should().Contain(e => e.Name == "Squat");
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldSortExercisesByIdForEachTemplate()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Ordered Workout", "PREBUILT");
        this.testDataHelper.InsertTemplateExercise(ThirdExerciseId, PrebuiltTemplateId, "Third Exercise", "CORE", DefaultTargetSets, DefaultTargetReps, DefaultTargetWeight);
        this.testDataHelper.InsertTemplateExercise(FirstExerciseId, PrebuiltTemplateId, "First Exercise", "CHEST", DefaultTargetSets, DefaultTargetReps, DefaultTargetWeight);
        this.testDataHelper.InsertTemplateExercise(SecondExerciseId, PrebuiltTemplateId, "Second Exercise", "LEGS", DefaultTargetSets, DefaultTargetReps, DefaultTargetWeight);

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        var exercises = availableWorkouts[0].GetExercises();
        exercises[0].Id.Should().Be(FirstExerciseId);
        exercises[1].Id.Should().Be(SecondExerciseId);
        exercises[2].Id.Should().Be(ThirdExerciseId);
    }
}

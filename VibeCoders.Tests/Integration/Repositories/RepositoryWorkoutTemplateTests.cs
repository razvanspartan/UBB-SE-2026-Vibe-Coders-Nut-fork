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
    private const int FourthExerciseId = 103;
    
    private const int ExpectedZeroTemplates = 0;
    private const int ExpectedOneTemplate = 1;
    private const int ExpectedTwoTemplates = 2;
    private const int ExpectedThreeTemplates = 3;
    private const int ExpectedZeroExercises = 0;
    private const int ExpectedOneExercise = 1;
    private const int ExpectedTwoExercises = 2;

    private const int DefaultTargetSets = 3;
    private const int DefaultTargetReps = 10;
    private const double DefaultTargetWeight = 50.0;
    private const double UpdatedTargetWeight = 75.0;

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
    public void GetAvailableWorkouts_ShouldReturnOnlyPrebuiltTemplates_WhenClientHasNoCustomOrTrainerAssignedWorkouts()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Push Day", "PREBUILT");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        availableWorkouts[0].Type.Should().Be(WorkoutType.PREBUILT);
        availableWorkouts[0].Name.Should().Be("Push Day");
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldIncludeCustomTemplates_ForSpecificClient()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertClient(SecondaryClientId);
        this.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "My Custom Workout", "CUSTOM");
        this.InsertWorkoutTemplate(DifferentClientCustomTemplateId, SecondaryClientId, "Other Client Custom", "CUSTOM");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        availableWorkouts[0].Type.Should().Be(WorkoutType.CUSTOM);
        availableWorkouts[0].ClientId.Should().Be(PrimaryClientId);
        availableWorkouts[0].Name.Should().Be("My Custom Workout");
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldIncludeTrainerAssignedTemplates_ForSpecificClient()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertClient(SecondaryClientId);
        this.InsertWorkoutTemplate(TrainerAssignedTemplateId, PrimaryClientId, "Trainer Program", "TRAINERASSIGNED");
        this.InsertWorkoutTemplate(DifferentClientCustomTemplateId, SecondaryClientId, "Other Trainer Program", "TRAINERASSIGNED");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        availableWorkouts[0].Type.Should().Be(WorkoutType.TRAINER_ASSIGNED);
        availableWorkouts[0].ClientId.Should().Be(PrimaryClientId);
        availableWorkouts[0].Name.Should().Be("Trainer Program");
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldHandleVariousTypeFormatting_WithUnderscoresAndHyphens()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Prebuilt Workout", "PRE_BUILT");
        this.InsertWorkoutTemplate(TrainerAssignedTemplateId, PrimaryClientId, "Assigned Workout", "TRAINER-ASSIGNED");

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
        this.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Universal Strength", "PREBUILT");
        this.InsertWorkoutTemplate(TrainerAssignedTemplateId, PrimaryClientId, "Custom Plan", "TRAINERASSIGNED");
        this.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "Personal Routine", "CUSTOM");
        this.InsertWorkoutTemplate(DifferentClientCustomTemplateId, SecondaryClientId, "Another Custom", "CUSTOM");

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
        this.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Full Body", "PREBUILT");
        this.InsertTemplateExercise(FirstExerciseId, PrebuiltTemplateId, "Bench Press", "CHEST");
        this.InsertTemplateExercise(SecondExerciseId, PrebuiltTemplateId, "Squat", "LEGS");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        availableWorkouts[0].GetExercises().Should().HaveCount(ExpectedTwoExercises);
        availableWorkouts[0].GetExercises().Should().Contain(e => e.Name == "Bench Press");
        availableWorkouts[0].GetExercises().Should().Contain(e => e.Name == "Squat");
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldHandleTemplatesWithNoExercises()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "Empty Workout", "CUSTOM");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        availableWorkouts[0].GetExercises().Should().HaveCount(ExpectedZeroExercises);
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldReturnEmptyList_WhenClientHasNoAccessibleTemplates()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.testDataHelper.InsertClient(SecondaryClientId);
        this.InsertWorkoutTemplate(CustomTemplateId, SecondaryClientId, "Other Client Workout", "CUSTOM");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedZeroTemplates);
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldLoadMultipleExercisesWithCorrectProperties()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "Strength Program", "CUSTOM");
        this.InsertTemplateExercise(FirstExerciseId, CustomTemplateId, "Deadlift", "BACK");
        this.InsertTemplateExercise(SecondExerciseId, CustomTemplateId, "Overhead Press", "SHOULDERS");
        this.InsertTemplateExercise(ThirdExerciseId, CustomTemplateId, "Pull Ups", "BACK");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        availableWorkouts.Should().HaveCount(ExpectedOneTemplate);
        var exercises = availableWorkouts[0].GetExercises();
        exercises.Should().HaveCount(ExpectedThreeTemplates);

        var deadlift = exercises.FirstOrDefault(e => e.Name == "Deadlift");
        deadlift.Should().NotBeNull();
        deadlift!.MuscleGroup.Should().Be(MuscleGroup.BACK);
        deadlift.WorkoutTemplateId.Should().Be(CustomTemplateId);
    }

    [Fact]
    public void GetAvailableWorkouts_ShouldSortExercisesByIdForEachTemplate()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(PrebuiltTemplateId, PrimaryClientId, "Ordered Workout", "PREBUILT");
        this.InsertTemplateExercise(ThirdExerciseId, PrebuiltTemplateId, "Third Exercise", "CORE");
        this.InsertTemplateExercise(FirstExerciseId, PrebuiltTemplateId, "First Exercise", "CHEST");
        this.InsertTemplateExercise(SecondExerciseId, PrebuiltTemplateId, "Second Exercise", "LEGS");

        var availableWorkouts = this.repository.GetAvailableWorkouts(PrimaryClientId);

        var exercises = availableWorkouts[0].GetExercises();
        exercises[0].Id.Should().Be(FirstExerciseId);
        exercises[1].Id.Should().Be(SecondExerciseId);
        exercises[2].Id.Should().Be(ThirdExerciseId);
    }

    [Fact]
    public void GetTemplateExercise_ShouldReturnExerciseWithAllProperties_WhenExerciseExists()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "Test Workout", "CUSTOM");
        this.InsertTemplateExercise(FirstExerciseId, CustomTemplateId, "Barbell Row", "BACK");

        var exercise = this.repository.GetTemplateExercise(FirstExerciseId);

        exercise.Should().NotBeNull();
        exercise!.Id.Should().Be(FirstExerciseId);
        exercise.Name.Should().Be("Barbell Row");
        exercise.WorkoutTemplateId.Should().Be(CustomTemplateId);
        exercise.MuscleGroup.Should().Be(MuscleGroup.BACK);
        exercise.TargetSets.Should().Be(DefaultTargetSets);
        exercise.TargetReps.Should().Be(DefaultTargetReps);
        exercise.TargetWeight.Should().Be(DefaultTargetWeight);
    }

    [Fact]
    public void GetTemplateExercise_ShouldReturnNull_WhenExerciseDoesNotExist()
    {
        const int nonExistentExerciseId = 9999;

        var exercise = this.repository.GetTemplateExercise(nonExistentExerciseId);

        exercise.Should().BeNull();
    }

    [Fact]
    public void UpdateTemplateWeight_ShouldUpdateWeight_WhenExerciseExists()
    {
        this.testDataHelper.InsertClient(PrimaryClientId);
        this.InsertWorkoutTemplate(CustomTemplateId, PrimaryClientId, "Progressive Workout", "CUSTOM");
        this.InsertTemplateExercise(FirstExerciseId, CustomTemplateId, "Bench Press", "CHEST");

        var updateResult = this.repository.UpdateTemplateWeight(FirstExerciseId, UpdatedTargetWeight);

        updateResult.Should().BeTrue();

        var updatedExercise = this.repository.GetTemplateExercise(FirstExerciseId);
        updatedExercise.Should().NotBeNull();
        updatedExercise!.TargetWeight.Should().Be(UpdatedTargetWeight);
    }

    [Fact]
    public void UpdateTemplateWeight_ShouldReturnFalse_WhenExerciseDoesNotExist()
    {
        const int nonExistentExerciseId = 9999;

        var updateResult = this.repository.UpdateTemplateWeight(nonExistentExerciseId, UpdatedTargetWeight);

        updateResult.Should().BeFalse();
    }

    private void InsertWorkoutTemplate(int templateId, int clientId, string name, string type)
    {
        using var command = new SqliteCommand(
            "INSERT INTO WORKOUT_TEMPLATE (workout_template_id, client_id, name, type) VALUES (@templateId, @clientId, @name, @type)",
            this.connection);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", type);
        command.ExecuteNonQuery();
    }

    private void InsertTemplateExercise(int exerciseId, int templateId, string name, string muscleGroup)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO TEMPLATE_EXERCISE (id, workout_template_id, name, muscle_group, target_sets, target_reps, target_weight)
              VALUES (@exerciseId, @templateId, @name, @muscleGroup, @targetSets, @targetReps, @targetWeight)",
            this.connection);
        command.Parameters.AddWithValue("@exerciseId", exerciseId);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@muscleGroup", muscleGroup);
        command.Parameters.AddWithValue("@targetSets", DefaultTargetSets);
        command.Parameters.AddWithValue("@targetReps", DefaultTargetReps);
        command.Parameters.AddWithValue("@targetWeight", DefaultTargetWeight);
        command.ExecuteNonQuery();
    }
}

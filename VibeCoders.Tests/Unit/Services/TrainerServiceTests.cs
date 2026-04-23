using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using VibeCoders.Tests.Mocks.DataFactories;
using Xunit;

namespace VibeCoders.Tests.Unit.Services;

public class TrainerServiceTests
{
    private readonly IRepositoryWorkoutTemplate mockWorkoutTemplateRepository;
    private readonly IRepositoryWorkoutLog mockWorkoutLogRepository;
    private readonly IRepositoryTrainer mockTrainerRepository;
    private readonly IRepositoryNutrition mockNutritionRepository;
    private readonly TrainerService trainerService;

    public TrainerServiceTests()
    {
        this.mockWorkoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();
        this.mockWorkoutLogRepository = Substitute.For<IRepositoryWorkoutLog>();
        this.mockTrainerRepository = Substitute.For<IRepositoryTrainer>();
        this.mockNutritionRepository = Substitute.For<IRepositoryNutrition>();

        this.trainerService = new TrainerService(
            this.mockWorkoutTemplateRepository,
            this.mockWorkoutLogRepository,
            this.mockTrainerRepository,
            this.mockNutritionRepository);
    }

    [Fact]
    public void SaveWorkoutFeedback_Should_ReturnFalse_When_LogIsNull()
    {
        bool result = this.trainerService.SaveWorkoutFeedback(null!);
        result.Should().BeFalse();
    }

    [Fact]
    public void SaveWorkoutFeedback_Should_CallRepository_When_LogIsValid()
    {
        var log = new WorkoutLog { Id = 10, Rating = 5, TrainerNotes = "Good job" };
        this.mockWorkoutLogRepository.UpdateWorkoutLogFeedback(10, 5, "Good job").Returns(true);

        bool result = this.trainerService.SaveWorkoutFeedback(log);

        result.Should().BeTrue();
        this.mockWorkoutLogRepository.Received(1).UpdateWorkoutLogFeedback(10, 5, "Good job");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssignNewRoutine_Should_ReturnError_When_RoutineNameIsInvalid(string? invalidName)
    {
        var exercises = new List<TemplateExercise> { new TemplateExercise { Name = "Pushup" } };

        var result = this.trainerService.AssignNewRoutine(null, 1, invalidName!, exercises);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Routine Name cannot be empty.");
    }

    [Fact]
    public void AssignNewRoutine_Should_ReturnError_When_ExercisesAreEmpty()
    {
        var result = this.trainerService.AssignNewRoutine(null, 1, "My Routine", Enumerable.Empty<TemplateExercise>());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("You must add at least one exercise to the routine.");
    }

    [Fact]
    public void AssignNewRoutine_Should_ReturnSuccess_And_SaveToRepository_When_Valid()
    {
        var exercises = new List<TemplateExercise> { new TemplateExercise { Name = "Squat" } };
        this.mockTrainerRepository.SaveTrainerWorkout(Arg.Any<WorkoutTemplate>()).Returns(true);

        var result = this.trainerService.AssignNewRoutine(null, 1, "Leg Day", exercises);

        result.Success.Should().BeTrue();
        this.mockTrainerRepository.Received(1).SaveTrainerWorkout(Arg.Is<WorkoutTemplate>(t => t.Name == "Leg Day"));
    }

    [Fact]
    public void AssignNutritionPlan_Should_ReturnFalse_When_ClientIdIsInvalid()
    {
        var plan = new NutritionPlan();
        bool result = this.trainerService.AssignNutritionPlan(plan, 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void AssignNutritionPlan_Should_CallRepository_When_DataIsValid()
    {
        var plan = new NutritionPlan();
        int validClientId = 5;

        bool result = this.trainerService.AssignNutritionPlan(plan, validClientId);

        result.Should().BeTrue();
        this.mockNutritionRepository.Received(1).SaveNutritionPlanForClient(plan, validClientId);
    }
    [Fact]
    public void AssignNewRoutine_Should_ReturnError_When_DatabaseSaveFails()
    {
        
        var exercises = new List<TemplateExercise> { new TemplateExercise { Name = "Pushup" } };
       
        this.mockTrainerRepository.SaveTrainerWorkout(Arg.Any<WorkoutTemplate>()).Returns(false);

    
        var result = this.trainerService.AssignNewRoutine(null, 1, "Fail Routine", exercises);

    
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Could not save routine to database.");
    }

    [Fact]
    public void AssignWorkout_Should_ThrowNotImplementedException()
    {
      
        var client = new Client();
        var log = new WorkoutLog();

        Action act = () => this.trainerService.AssignWorkout(client, log);

        act.Should().Throw<NotImplementedException>()
           .WithMessage("Workout assignment coming in Slice 2!");
    }


    [Fact]
    public void CreateAndAssignNutritionPlan_Should_CreatePlanWithDateOnly()
    {
       
        var start = new DateTime(2026, 1, 1, 10, 30, 0); // 10:30 AM
        var end = new DateTime(2026, 1, 7, 18, 0, 0);   // 6:00 PM
        int clientId = 99;

      
        this.trainerService.CreateAndAssignNutritionPlan(start, end, clientId);

       
        this.mockNutritionRepository.Received(1).SaveNutritionPlanForClient(
            Arg.Is<NutritionPlan>(p => p.StartDate == new DateTime(2026, 1, 1) && p.EndDate == new DateTime(2026, 1, 7)),
            clientId);
    }


    [Fact]
    public void CreateAndAssignNutritionPlan_Should_SetDatesToMidnight()
    {
        var start = new DateTime(2026, 1, 1, 14, 30, 0);
        var end = new DateTime(2026, 1, 7, 14, 30, 0);
        int clientId = 1;

        this.trainerService.CreateAndAssignNutritionPlan(start, end, clientId);

        this.mockNutritionRepository.Received(1).SaveNutritionPlanForClient(
            Arg.Is<NutritionPlan>(p => p.StartDate == start.Date && p.EndDate == end.Date),
            clientId);
    }

    [Fact]
    public void GetClientWorkoutHistory_Should_ReturnLogsFromRepository()
    {
        var mockLogs = new List<WorkoutLog> { WorkoutLogFactory.CreateHighIntensityWorkoutLog() };
        this.mockWorkoutLogRepository.GetWorkoutHistory(1).Returns(mockLogs);

        var result = this.trainerService.GetClientWorkoutHistory(1);

        result.Should().HaveCount(1);
        result.First().WorkoutName.Should().Be("High Intensity Strength Session");
    }
    [Fact]
    public void SaveTrainerWorkout_Should_ReturnFalse_When_TemplateIsNull()
    {
       
        bool result = this.trainerService.SaveTrainerWorkout(null!);

       
        result.Should().BeFalse();
    }


}

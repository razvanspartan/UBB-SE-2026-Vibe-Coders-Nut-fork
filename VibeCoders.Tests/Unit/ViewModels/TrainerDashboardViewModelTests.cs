using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.UI.Xaml;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class TrainerDashboardViewModelTests
    {
        private readonly ITrainerService trainerServiceMock;
                private readonly INavigationService navigationMock;
        private readonly TrainerDashboardViewModel systemUnderTest;

        public TrainerDashboardViewModelTests()
        {
            this.trainerServiceMockMock = Substitute.For<ITrainerService>();
            
            var clientList = new List<Client> { new Client { Id = 1, Username = "John" } };
            this.trainerServiceMockMock.GetTrainerClient(1).Returns(clientList);
            this.trainerServiceMockMock.GetAllExerciseNames().Returns(new List<string> { "Squat" });

            
            this.navigationMock = Substitute.For<INavigationService>();

            this.systemUnderTest = new TrainerDashboardViewModel(this.trainerServiceMock, this.navigationMock);
        }

        [Fact]
        public void Constructor_InitializesState()
        {
            this.systemUnderTest.AssignedClients.Should().HaveCount(1);
            this.systemUnderTest.SelectedClient.Should().NotBeNull();
            this.systemUnderTest.SelectedClient?.Id.Should().Be(1);
            this.systemUnderTest.AvailableExercises.Should().Contain("Squat");
        }

        [Fact]
        public void SelectedClient_WhenChanged_ReloadsData()
        {
            var log = new WorkoutLog { Id = 1 };
            this.trainerServiceMockMock.GetWorkoutHistory(1).Returns(new List<WorkoutLog> { log });
            
            var template = new WorkoutTemplate { Type = WorkoutType.TRAINER_ASSIGNED };
            this.trainerServiceMockMock.GetAvailableWorkouts(1).Returns(new List<WorkoutTemplate> { template });

            var newClient = new Client { Id = 1 };
            this.systemUnderTest.SelectedClient = newClient;

            this.systemUnderTest.SelectedClientLogs.Should().ContainSingle();
            this.systemUnderTest.AssignedWorkouts.Should().ContainSingle();
            this.systemUnderTest.SelectedWorkoutLog.Should().Be(log);
        }

        [Fact]
        public void LoadLogsForSelectedClient_WhenNullClient_ClearsLogs()
        {
            this.systemUnderTest.SelectedClient = null;

            this.systemUnderTest.LoadLogsForSelectedClient();

            this.systemUnderTest.SelectedClientLogs.Should().BeEmpty();
            this.systemUnderTest.CurrentWorkoutDetails.Should().BeEmpty();
            this.systemUnderTest.SelectedWorkoutLog.Should().BeNull();
        }

        [Fact]
        public void DateRangeProperties_TestValidation()
        {
            this.systemUnderTest.PlanStartDate = DateTimeOffset.Now;
            this.systemUnderTest.PlanEndDate = DateTimeOffset.Now.AddDays(-1);

            this.systemUnderTest.HasDateRangeError.Should().BeTrue();
            this.systemUnderTest.DateRangeError.Should().NotBeEmpty();
            this.systemUnderTest.CanAssignPlan.Should().BeFalse();

            this.systemUnderTest.PlanEndDate = DateTimeOffset.Now.AddDays(5);
            this.systemUnderTest.HasDateRangeError.Should().BeFalse();
            this.systemUnderTest.DateRangeError.Should().BeEmpty();
            
            this.systemUnderTest.SelectedClient = new Client { Id = 1 };
            this.systemUnderTest.CanAssignPlan.Should().BeTrue();
        }

        [Fact]
        public void PrepareForEdit_PopulatesBuilder()
        {
            var template = new WorkoutTemplate { Id = 5, Name = "Plan A" };
            var exercise = new TemplateExercise { Name = "Pushup" };
            template.AddExercise(exercise);

            this.systemUnderTest.PrepareForEdit(template);

            this.systemUnderTest.EditingTemplateId.Should().Be(5);
            this.systemUnderTest.NewRoutineName.Should().Be("Plan A");
            this.systemUnderTest.BuilderExercises.Should().Contain(exercise);
        }

        [Fact]
        public void DeleteRoutine_WhenSuccessful_RemovesFromList()
        {
            var template = new WorkoutTemplate { Id = 1 };
            this.systemUnderTest.AssignedWorkouts.Add(template);
            this.trainerServiceMockMock.DeleteWorkoutTemplate(1).Returns(true);

            var result = this.systemUnderTest.DeleteRoutine(template);

            result.Should().BeTrue();
            this.systemUnderTest.AssignedWorkouts.Should().BeEmpty();
        }

        [Fact]
        public void DeleteRoutine_WhenNull_ReturnsFalse()
        {
            var result = this.systemUnderTest.DeleteRoutine(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void SaveRoutine_ReturnsServiceResult()
        {
            var template = new WorkoutTemplate();
            this.trainerServiceMockMock.SaveTrainerWorkout(template).Returns(true);

            var result = this.systemUnderTest.SaveRoutine(template);

            result.Should().BeTrue();
        }

        [Fact]
        public void AddExerciseToRoutine_WhenValid_AddsToBuilder()
        {
            this.systemUnderTest.SelectedNewExercise = "Squat";
            this.systemUnderTest.NewExerciseSets = 5;
            this.systemUnderTest.NewExerciseReps = 5;
            this.systemUnderTest.NewExerciseWeight = 100;

            this.systemUnderTest.AddExerciseToRoutine(null!, null!);

            this.systemUnderTest.BuilderExercises.Should().ContainSingle();
            var ex = this.systemUnderTest.BuilderExercises.First();
            ex.Name.Should().Be("Squat");
            ex.TargetSets.Should().Be(5);
            ex.TargetReps.Should().Be(5);
            ex.TargetWeight.Should().Be(100);
            this.systemUnderTest.SelectedNewExercise.Should().BeNull();
        }

        [Fact]
        public void AddExerciseToRoutine_WhenEmpty_DoesNothing()
        {
            this.systemUnderTest.SelectedNewExercise = "";
            this.systemUnderTest.AddExerciseToRoutine(null!, null!);
            this.systemUnderTest.BuilderExercises.Should().BeEmpty();
        }

        [Fact]
        public void RemoveExerciseFromRoutine_RemovesItem()
        {
            var exercise = new TemplateExercise { Name = "Pullup" };
            this.systemUnderTest.BuilderExercises.Add(exercise);

            this.systemUnderTest.RemoveExerciseFromRoutine(exercise);

            this.systemUnderTest.BuilderExercises.Should().BeEmpty();
        }

        [Fact]
        public void SaveCurrentFeedback_WhenValid_UpdatesStorageAndHidesForm()
        {
            var log = new WorkoutLog { Id = 1, Rating = 5 };
            this.systemUnderTest.SelectedWorkoutLog = log;

            this.trainerServiceMockMock.UpdateWorkoutLogFeedback(1, 5, Arg.Any<string>()).Returns(true);

            this.systemUnderTest.SaveCurrentFeedback(null!, null!);

            this.trainerServiceMockMock.Received(1).UpdateWorkoutLogFeedback(1, 5, log.TrainerNotes);
            this.systemUnderTest.IsFeedbackFormVisible.Should().BeFalse();
            this.systemUnderTest.FeedbackErrorText.Should().BeEmpty();
        }

        [Fact]
        public void SaveCurrentFeedback_WhenRatingInvalid_ShowsError()
        {
            var log = new WorkoutLog { Id = 1, Rating = 0 };
            this.systemUnderTest.SelectedWorkoutLog = log;

            this.systemUnderTest.SaveCurrentFeedback(null!, null!);

            this.trainerServiceMockMock.DidNotReceive().UpdateWorkoutLogFeedback(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
            this.systemUnderTest.FeedbackErrorText.Should().Be("You cannot assign an empty feedback. Please select a star rating.");
        }

        [Fact]
        public void SaveCurrentFeedback_WhenNullLog_DoesNothing()
        {
            this.systemUnderTest.SelectedWorkoutLog = null;
            this.systemUnderTest.SaveCurrentFeedback(null!, null!);
            this.trainerServiceMockMock.DidNotReceiveWithAnyArgs().UpdateWorkoutLogFeedback(default, default, default!);
        }

        [Fact]
        public void AssignNutritionPlan_WhenValid_UpdatesStatus()
        {
            this.systemUnderTest.SelectedClient = new Client { Id = 1, Username = "TestUser" };
            this.systemUnderTest.PlanStartDate = DateTimeOffset.Now;
            this.systemUnderTest.PlanEndDate = DateTimeOffset.Now.AddDays(7);

            this.systemUnderTest.AssignNutritionPlanCommand.Execute(null);

            this.trainerServiceMockMock.Received(1).SaveNutritionPlanForClient(Arg.Any<NutritionPlan>(), 1);
            this.systemUnderTest.AssignmentStatus.Should().Contain("Plan assigned to TestUser");
        }

        [Fact]
        public void NavigationCommands_ExecuteExpectedRoutes()
        {
            this.systemUnderTest.SelectedClient = new Client { Id = 5 };

            this.systemUnderTest.OpenClientProfileCommand.Execute(null);
            this.navigationMock.Received(1).NavigateToClientProfile(5);

            this.systemUnderTest.OpenWorkoutLogsCommand.Execute(null);
            this.navigationMock.Received(1).NavigateToWorkoutLogs();

            this.systemUnderTest.OpenCalendarCommand.Execute(null);
            this.navigationMock.Received(1).NavigateToCalendarIntegration();
        }

        [Fact]
        public void Properties_Setters_UpdateValuesAndErrors()
        {
            this.systemUnderTest.BuilderErrorText = "Error 1";
            this.systemUnderTest.HasBuilderError.Should().BeTrue();

            this.systemUnderTest.BuilderErrorText = string.Empty;
            this.systemUnderTest.HasBuilderError.Should().BeFalse();

            this.systemUnderTest.NewRoutineName = "Best Routine";
            this.systemUnderTest.NewRoutineName.Should().Be("Best Routine");
        }

        [Fact]
        public void OnWorkoutLogSelected_UpdatesCurrentDetails()
        {
            var log = new WorkoutLog { Rating = 0 };
            log.Exercises.Add(new LoggedExercise { ExerciseName = "Squat", Sets = new List<LoggedSet> { new LoggedSet() } });

            this.systemUnderTest.SelectedWorkoutLog = log;

            this.systemUnderTest.CurrentWorkoutDetails.Should().ContainSingle();
            this.systemUnderTest.CurrentWorkoutDetails[0].Name.Should().Be("Squat");
            this.systemUnderTest.IsFeedbackFormVisible.Should().BeTrue();

            var ratedLog = new WorkoutLog { Rating = 4 };
            this.systemUnderTest.SelectedWorkoutLog = ratedLog;
            this.systemUnderTest.IsFeedbackFormVisible.Should().BeFalse();
        }
    }
}


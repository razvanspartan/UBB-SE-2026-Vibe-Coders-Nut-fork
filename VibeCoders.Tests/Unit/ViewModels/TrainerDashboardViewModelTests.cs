using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services.Interfaces;
using VibeCoders.Services;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class TrainerDashboardViewModelTests
    {
        private const int DefaultTrainerId = 1;
        private const int DefaultClientId = 1;
        private const int AlternateClientId = 5;
        private const int InvalidRating = 0;
        private const int ValidRating = 5;
        private const int NutritionPlanDurationDays = 7;
        private const int InvalidPlanOffsetDays = -1;
        private const int ValidPlanOffsetDays = 5;
        private const int TargetSetCount = 5;
        private const int TargetRepCount = 5;
        private const double TargetWeight = 100;
        private const int FirstDetailIndex = 0;
        private const int RoutineTemplateId = 5;
        private const string AssignedClientUsername = "John";
        private const string TestUserName = "TestUser";
        private const string PrimaryExerciseName = "Squat";
        private const string SecondaryExerciseName = "Pushup";
        private const string PullupExerciseName = "Pullup";
        private const string DefaultRoutineName = "Plan A";
        private const string UpdatedRoutineName = "Best Routine";
        private const string BuilderErrorMessage = "Error 1";

        private readonly ITrainerService trainerServiceMock;
        private readonly INavigationService navigationMock;
        private readonly TrainerDashboardViewModel trainerDashboardViewModel;

        public TrainerDashboardViewModelTests()
        {
            this.trainerServiceMock = Substitute.For<ITrainerService>();

            var assignedClients = new List<Client> { new Client { Id = DefaultClientId, Username = AssignedClientUsername } };
            this.trainerServiceMock.GetAssignedClients(DefaultTrainerId).Returns(assignedClients);
            this.trainerServiceMock.GetClientWorkoutHistory(DefaultClientId).Returns(new List<WorkoutLog>());
            this.trainerServiceMock.GetAvailableWorkouts(DefaultClientId).Returns(new List<WorkoutTemplate>());
            this.trainerServiceMock.GetAllExerciseNames().Returns(new List<string> { PrimaryExerciseName });

            this.navigationMock = Substitute.For<INavigationService>();

            this.trainerDashboardViewModel = new TrainerDashboardViewModel(this.trainerServiceMock, this.navigationMock);
        }

        [Fact]
        public void Constructor_InitializesState()
        {
            this.trainerDashboardViewModel.AssignedClients.Should().HaveCount(1);
            this.trainerDashboardViewModel.SelectedClient.Should().NotBeNull();
            this.trainerDashboardViewModel.SelectedClient?.Id.Should().Be(DefaultClientId);
            this.trainerDashboardViewModel.FilteredAvailableExercises.Should().Contain(PrimaryExerciseName);
        }

        [Fact]
        public void SelectedClient_WhenChanged_ReloadsData()
        {
            var workoutLog = new WorkoutLog { Id = DefaultClientId };
            this.trainerServiceMock.GetClientWorkoutHistory(DefaultClientId).Returns(new List<WorkoutLog> { workoutLog });

            var assignedWorkoutTemplate = new WorkoutTemplate { Type = WorkoutType.TRAINER_ASSIGNED };
            this.trainerServiceMock.GetAvailableWorkouts(DefaultClientId).Returns(new List<WorkoutTemplate> { assignedWorkoutTemplate });

            var selectedClient = new Client { Id = DefaultClientId };
            this.trainerDashboardViewModel.SelectedClient = selectedClient;

            this.trainerDashboardViewModel.SelectedClientLogs.Should().ContainSingle();
            this.trainerDashboardViewModel.ClientAssignedWorkouts.Should().ContainSingle();
            this.trainerDashboardViewModel.SelectedWorkoutLog.Should().Be(workoutLog);
        }

        [Fact]
        public void LoadLogsForSelectedClient_WhenNullClient_ClearsLogs()
        {
            this.trainerDashboardViewModel.SelectedClient = null;

            this.trainerDashboardViewModel.LoadLogsForSelectedClient();

            this.trainerDashboardViewModel.SelectedClientLogs.Should().BeEmpty();
            this.trainerDashboardViewModel.CurrentWorkoutDetails.Should().BeEmpty();
            this.trainerDashboardViewModel.SelectedWorkoutLog.Should().BeNull();
        }

        [Fact]
        public void DateRangeProperties_TestValidation()
        {
            this.trainerDashboardViewModel.PlanStartDate = DateTimeOffset.Now;
            this.trainerDashboardViewModel.PlanEndDate = DateTimeOffset.Now.AddDays(InvalidPlanOffsetDays);

            this.trainerDashboardViewModel.HasDateRangeError.Should().BeTrue();
            this.trainerDashboardViewModel.DateRangeError.Should().NotBeEmpty();
            this.trainerDashboardViewModel.CanAssignPlan.Should().BeFalse();

            this.trainerDashboardViewModel.PlanEndDate = DateTimeOffset.Now.AddDays(ValidPlanOffsetDays);
            this.trainerDashboardViewModel.HasDateRangeError.Should().BeFalse();
            this.trainerDashboardViewModel.DateRangeError.Should().BeEmpty();

            this.trainerDashboardViewModel.SelectedClient = new Client { Id = DefaultClientId };
            this.trainerDashboardViewModel.CanAssignPlan.Should().BeTrue();
        }

        [Fact]
        public void PrepareForEdit_PopulatesBuilder()
        {
            var workoutTemplate = new WorkoutTemplate { Id = RoutineTemplateId, Name = DefaultRoutineName };
            var templateExercise = new TemplateExercise { Name = SecondaryExerciseName };
            workoutTemplate.AddExercise(templateExercise);

            this.trainerDashboardViewModel.PrepareForEdit(workoutTemplate);

            this.trainerDashboardViewModel.EditingTemplateId.Should().Be(RoutineTemplateId);
            this.trainerDashboardViewModel.NewRoutineName.Should().Be(DefaultRoutineName);
            this.trainerDashboardViewModel.RoutineBuilderExercises.Should().Contain(templateExercise);
        }

        [Fact]
        public void DeleteRoutine_WhenSuccessful_RemovesFromList()
        {
            var workoutTemplate = new WorkoutTemplate { Id = DefaultClientId };
            this.trainerDashboardViewModel.ClientAssignedWorkouts.Add(workoutTemplate);
            this.trainerServiceMock.DeleteWorkoutTemplate(DefaultClientId).Returns(true);

            var result = this.trainerDashboardViewModel.DeleteRoutine(workoutTemplate);

            result.Should().BeTrue();
            this.trainerDashboardViewModel.ClientAssignedWorkouts.Should().BeEmpty();
        }

        [Fact]
        public void DeleteRoutine_WhenNull_ReturnsFalse()
        {
            var result = this.trainerDashboardViewModel.DeleteRoutine(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void BuildAndSaveRoutine_ReturnsServiceResult()
        {
            this.trainerDashboardViewModel.NewRoutineName = DefaultRoutineName;
            this.trainerServiceMock.AssignNewRoutine(0, DefaultClientId, DefaultRoutineName, this.trainerDashboardViewModel.RoutineBuilderExercises)
                .Returns((true, string.Empty));

            var result = this.trainerDashboardViewModel.BuildAndSaveRoutine();

            result.Should().BeTrue();
        }

        [Fact]
        public void AddExerciseToRoutine_WhenValid_AddsToBuilder()
        {
            this.trainerDashboardViewModel.SelectedNewExercise = PrimaryExerciseName;
            this.trainerDashboardViewModel.NewExerciseSets = TargetSetCount;
            this.trainerDashboardViewModel.NewExerciseReps = TargetRepCount;
            this.trainerDashboardViewModel.NewExerciseWeight = TargetWeight;

            this.trainerDashboardViewModel.AddExerciseToRoutine(null!, null!);

            this.trainerDashboardViewModel.RoutineBuilderExercises.Should().ContainSingle();
            var routineExercise = this.trainerDashboardViewModel.RoutineBuilderExercises.First();
            routineExercise.Name.Should().Be(PrimaryExerciseName);
            routineExercise.TargetSets.Should().Be(TargetSetCount);
            routineExercise.TargetReps.Should().Be(TargetRepCount);
            routineExercise.TargetWeight.Should().Be(TargetWeight);
            this.trainerDashboardViewModel.SelectedNewExercise.Should().BeNull();
        }

        [Fact]
        public void AddExerciseToRoutine_WhenEmpty_DoesNothing()
        {
            this.trainerDashboardViewModel.SelectedNewExercise = string.Empty;
            this.trainerDashboardViewModel.AddExerciseToRoutine(null!, null!);
            this.trainerDashboardViewModel.RoutineBuilderExercises.Should().BeEmpty();
        }

        [Fact]
        public void RemoveExerciseFromRoutine_RemovesItem()
        {
            var routineExercise = new TemplateExercise { Name = PullupExerciseName };
            this.trainerDashboardViewModel.RoutineBuilderExercises.Add(routineExercise);

            this.trainerDashboardViewModel.RemoveExerciseFromRoutine(routineExercise);

            this.trainerDashboardViewModel.RoutineBuilderExercises.Should().BeEmpty();
        }

        [Fact]
        public void SaveCurrentFeedback_WhenValid_UpdatesStorageAndHidesForm()
        {
            var workoutLog = new WorkoutLog { Id = DefaultClientId, Rating = ValidRating };
            this.trainerDashboardViewModel.SelectedWorkoutLog = workoutLog;

            this.trainerDashboardViewModel.SaveCurrentFeedback(null!, null!);

            this.trainerServiceMock.Received(1).SaveWorkoutFeedback(workoutLog);
            this.trainerDashboardViewModel.IsFeedbackFormVisible.Should().BeFalse();
            this.trainerDashboardViewModel.FeedbackErrorText.Should().BeEmpty();
        }

        [Fact]
        public void SaveCurrentFeedback_WhenRatingInvalid_ShowsError()
        {
            var workoutLog = new WorkoutLog { Id = DefaultClientId, Rating = InvalidRating };
            this.trainerDashboardViewModel.SelectedWorkoutLog = workoutLog;

            this.trainerDashboardViewModel.SaveCurrentFeedback(null!, null!);

            this.trainerServiceMock.DidNotReceive().SaveWorkoutFeedback(Arg.Any<WorkoutLog>());
            this.trainerDashboardViewModel.FeedbackErrorText.Should().Be("You cannot assign an empty feedback. Please select a star rating.");
        }

        [Fact]
        public void SaveCurrentFeedback_WhenNullLog_DoesNothing()
        {
            this.trainerDashboardViewModel.SelectedWorkoutLog = null;
            this.trainerDashboardViewModel.SaveCurrentFeedback(null!, null!);
            this.trainerServiceMock.DidNotReceiveWithAnyArgs().SaveWorkoutFeedback(default!);
        }

        [Fact]
        public void AssignNutritionPlan_WhenValid_UpdatesStatus()
        {
            this.trainerDashboardViewModel.SelectedClient = new Client { Id = DefaultClientId, Username = TestUserName };
            this.trainerDashboardViewModel.PlanStartDate = DateTimeOffset.Now;
            this.trainerDashboardViewModel.PlanEndDate = DateTimeOffset.Now.AddDays(NutritionPlanDurationDays);
            this.trainerServiceMock.CreateAndAssignNutritionPlan(
                this.trainerDashboardViewModel.PlanStartDate.Date,
                this.trainerDashboardViewModel.PlanEndDate.Date,
                DefaultClientId).Returns(true);

            this.trainerDashboardViewModel.AssignNutritionPlanCommand.Execute(null);

            this.trainerServiceMock.Received(1).CreateAndAssignNutritionPlan(
                this.trainerDashboardViewModel.PlanStartDate.Date,
                this.trainerDashboardViewModel.PlanEndDate.Date,
                DefaultClientId);
            this.trainerDashboardViewModel.AssignmentStatus.Should().Contain($"Plan assigned to {TestUserName}");
        }

        [Fact]
        public void NavigationCommands_ExecuteExpectedRoutes()
        {
            this.trainerServiceMock.GetClientWorkoutHistory(AlternateClientId).Returns(new List<WorkoutLog>());
            this.trainerServiceMock.GetAvailableWorkouts(AlternateClientId).Returns(new List<WorkoutTemplate>());
            this.trainerDashboardViewModel.SelectedClient = new Client { Id = AlternateClientId };

            this.trainerDashboardViewModel.OpenClientProfileCommand.Execute(null);
            this.navigationMock.Received(1).NavigateToClientProfile(AlternateClientId);

            this.trainerDashboardViewModel.OpenWorkoutLogsCommand.Execute(null);
            this.navigationMock.Received(1).NavigateToWorkoutLogs();

            this.trainerDashboardViewModel.OpenCalendarCommand.Execute(null);
            this.navigationMock.Received(1).NavigateToCalendarIntegration();
        }

        [Fact]
        public void Properties_Setters_UpdateValuesAndErrors()
        {
            this.trainerDashboardViewModel.BuilderErrorText = BuilderErrorMessage;
            this.trainerDashboardViewModel.HasBuilderError.Should().BeTrue();

            this.trainerDashboardViewModel.BuilderErrorText = string.Empty;
            this.trainerDashboardViewModel.HasBuilderError.Should().BeFalse();

            this.trainerDashboardViewModel.NewRoutineName = UpdatedRoutineName;
            this.trainerDashboardViewModel.NewRoutineName.Should().Be(UpdatedRoutineName);
        }

        [Fact]
        public void OnWorkoutLogSelected_UpdatesCurrentDetails()
        {
            var unratedWorkoutLog = new WorkoutLog { Rating = InvalidRating };
            unratedWorkoutLog.Exercises.Add(new LoggedExercise { ExerciseName = PrimaryExerciseName, Sets = new List<LoggedSet> { new LoggedSet() } });

            this.trainerDashboardViewModel.SelectedWorkoutLog = unratedWorkoutLog;

            this.trainerDashboardViewModel.CurrentWorkoutDetails.Should().ContainSingle();
            this.trainerDashboardViewModel.CurrentWorkoutDetails[FirstDetailIndex].Name.Should().Be(PrimaryExerciseName);
            this.trainerDashboardViewModel.IsFeedbackFormVisible.Should().BeTrue();

            var ratedWorkoutLog = new WorkoutLog { Rating = 4 };
            this.trainerDashboardViewModel.SelectedWorkoutLog = ratedWorkoutLog;
            this.trainerDashboardViewModel.IsFeedbackFormVisible.Should().BeFalse();
        }
    }
}


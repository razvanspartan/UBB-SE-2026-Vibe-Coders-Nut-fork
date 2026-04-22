using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class ActiveWorkoutViewModelTests
    {
        private const int DefaultClientId = 1;
        private const int DefaultTemplateId = 1;
        private const int DefaultNotificationId = 1;
        private const int SingleTargetSet = 1;
        private const int MultipleTargetSets = 2;
        private const int GoalTargetSets = 3;
        private const int CompletedSetReps = 10;
        private const double CompletedSetWeight = 100;
        private const double CurrentSetRepsInput = 12;
        private const double CurrentSetWeightInput = 80;

        private readonly IClientService clientServiceMock;
        private readonly INavigationService navigationMock;
        private readonly WorkoutUiState workoutState;
        private readonly ActiveWorkoutViewModel activeWorkoutViewModel;

        public ActiveWorkoutViewModelTests()
        {
            this.clientServiceMock = Substitute.For<IClientService>();
            this.navigationMock = Substitute.For<INavigationService>();
            this.workoutState = new WorkoutUiState();
            
            this.activeWorkoutViewModel = new ActiveWorkoutViewModel(
                this.clientServiceMock,
                this.navigationMock,
                this.workoutState);
        }

        private void StartWorkout(int clientId, int templateId, string workoutName, string exerciseName, int targetSets = SingleTargetSet)
        {
            this.activeWorkoutViewModel.SelectedGoal = workoutName;

            var workoutTemplate = new WorkoutTemplate { Id = templateId, Name = workoutName, Type = WorkoutType.CUSTOM };
            workoutTemplate.AddExercise(new TemplateExercise { Name = exerciseName, TargetSets = targetSets });

            this.clientServiceMock.GetAvailableWorkoutsForClient(clientId).Returns(new List<WorkoutTemplate> { workoutTemplate });

            this.activeWorkoutViewModel.ApplyTargetGoalsCommand.Execute(clientId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        public void SetRestTime_WhenInvalid_DoesNotStartTimer(string? restTimeInput)
        {
            this.activeWorkoutViewModel.SetRestTimeCommand.Execute(restTimeInput);
            this.activeWorkoutViewModel.RestTimeRemaining.Should().Be(0);
        }

        [Fact]
        public void StartRestTimer_WhenZeroOrNegative_StopsResting()
        {
            this.activeWorkoutViewModel.IsResting = true;
            this.activeWorkoutViewModel.StartRestTimer(0);
            this.activeWorkoutViewModel.IsResting.Should().BeFalse();
            this.activeWorkoutViewModel.RestTimeRemaining.Should().Be(0);
        }

        [Fact]
        public void ApplyTargetGoals_WhenValid_LoadsTemplateAndStartsWorkout()
        {
            this.activeWorkoutViewModel.SelectedGoal = "Push";
            var workoutTemplate = new WorkoutTemplate { Id = DefaultTemplateId, Name = "Push", Type = WorkoutType.CUSTOM };
            workoutTemplate.AddExercise(new TemplateExercise { Name = "Bench", TargetSets = GoalTargetSets });
            this.clientServiceMock.GetAvailableWorkoutsForClient(DefaultClientId).Returns(new List<WorkoutTemplate> { workoutTemplate });

            this.activeWorkoutViewModel.ApplyTargetGoalsCommand.Execute(DefaultClientId);

            this.activeWorkoutViewModel.IsWorkoutStarted.Should().BeTrue();
            this.activeWorkoutViewModel.WorkoutSessionTitle.Should().Be("Push");
            this.activeWorkoutViewModel.ExerciseRows.Should().HaveCount(1);
            this.activeWorkoutViewModel.CurrentExerciseName.Should().Be("Bench");
        }

        [Fact]
        public void SaveSet_WhenValid_SavesAndAdvances()
        {
            this.StartWorkout(DefaultClientId, DefaultTemplateId, "Squat", "Squat", MultipleTargetSets);
            var activeSet = this.activeWorkoutViewModel.ExerciseRows[0].Sets[0];
            activeSet.ActualReps = CompletedSetReps;
            activeSet.ActualWeight = CompletedSetWeight;
            this.clientServiceMock.SaveSet(Arg.Any<WorkoutLog>(), "Squat", Arg.Any<LoggedSet>()).Returns(true);

            this.activeWorkoutViewModel.SaveSetCommand.Execute(activeSet);

            activeSet.IsCompleted.Should().BeTrue();
            this.activeWorkoutViewModel.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public void SaveSet_WhenServiceFails_SetsErrorMessage()
        {
            this.StartWorkout(DefaultClientId, DefaultTemplateId, "Squat", "Squat", MultipleTargetSets);
            var activeSet = this.activeWorkoutViewModel.ExerciseRows[0].Sets[0];
            activeSet.ActualReps = CompletedSetReps;
            activeSet.ActualWeight = CompletedSetWeight;
            this.clientServiceMock.SaveSet(Arg.Any<WorkoutLog>(), "Squat", Arg.Any<LoggedSet>()).Returns(false);

            this.activeWorkoutViewModel.SaveSetCommand.Execute(activeSet);

            activeSet.IsCompleted.Should().BeFalse();
            this.activeWorkoutViewModel.ErrorMessage.Should().Be("Failed to save set. Please try again.");
        }

        [Fact]
        public void FinishWorkout_WhenValid_FinalizesAndNavigates()
        {
            this.StartWorkout(DefaultClientId, DefaultTemplateId, "Push", "Bench");
            this.clientServiceMock.FinalizeWorkout(Arg.Any<WorkoutLog>()).Returns(true);

            this.activeWorkoutViewModel.FinishWorkoutCommand.Execute(DefaultClientId);

            this.activeWorkoutViewModel.IsWorkoutStarted.Should().BeFalse();
            this.navigationMock.Received(1).NavigateToClientDashboard(true);
        }

        [Fact]
        public void LoadNotifications_PopulatesList()
        {
            var notification = new Notification { Id = DefaultNotificationId };
            this.clientServiceMock.GetNotifications(DefaultClientId).Returns(new List<Notification> { notification });

            this.activeWorkoutViewModel.LoadNotificationsCommand.Execute(DefaultClientId);

            this.activeWorkoutViewModel.Notifications.Should().ContainSingle().Which.Should().Be(notification);
        }

        [Fact]
        public void ConfirmDeload_RemovesNotification()
        {
            var notification = new Notification { Id = DefaultNotificationId };
            this.activeWorkoutViewModel.Notifications.Add(notification);

            this.activeWorkoutViewModel.ConfirmDeloadCommand.Execute(notification);

            this.clientServiceMock.Received(1).ConfirmDeload(notification);
            this.activeWorkoutViewModel.Notifications.Should().BeEmpty();
        }

        [Fact]
        public void CompleteCurrentSet_UpdatesPendingSet()
        {
            this.StartWorkout(DefaultClientId, DefaultTemplateId, "Push", "Bench");

            this.activeWorkoutViewModel.CurrentSetRepsInput = CurrentSetRepsInput;
            this.activeWorkoutViewModel.CurrentSetWeightInput = CurrentSetWeightInput;

            this.activeWorkoutViewModel.CompleteCurrentSetCommand.Execute(null);

            var activeSet = this.activeWorkoutViewModel.ExerciseRows[0].Sets[0];
            activeSet.ActualRepsValue.Should().Be(CurrentSetRepsInput);
            activeSet.ActualWeightValue.Should().Be(CurrentSetWeightInput);
        }

        [Fact]
        public void ActiveSetViewModel_Properties_FunctionCorrectly()
        {
            var autoSaveTriggered = false;
            var activeSet = new ActiveSetViewModel
            {
                AutoSaveHandler = _ => autoSaveTriggered = true
            };

            activeSet.ActualRepsValue = 10;
            activeSet.ActualWeightValue = double.NaN;

            activeSet.ActualReps.Should().Be(10);
            autoSaveTriggered.Should().BeFalse();

            activeSet.ActualWeightValue = 50.5;
            activeSet.ActualWeight.Should().Be(50.5);
            autoSaveTriggered.Should().BeTrue();
        }

        [Fact]
        public void ActiveSetViewModel_WhenValueIsNotANumber_SetsNull()
        {
            var activeSet = new ActiveSetViewModel();
            activeSet.ActualRepsValue = double.NaN;
            activeSet.ActualWeightValue = double.NaN;

            activeSet.ActualReps.Should().BeNull();
            activeSet.ActualWeight.Should().BeNull();
        }
    }
}


using System;
using System.Collections.Generic;
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
        private readonly IClientService clientServiceMock;
        private readonly INavigationService navigationMock;
        private readonly WorkoutUiState uiState;
        private readonly ActiveWorkoutViewModel systemUnderTest;

        public ActiveWorkoutViewModelTests()
        {
            this.clientServiceMock = Substitute.For<IClientService>();
            this.navigationMock = Substitute.For<INavigationService>();
            this.uiState = new WorkoutUiState();
            
            this.systemUnderTest = new ActiveWorkoutViewModel(
                this.clientServiceMock,
                this.storageMock,
                this.navigationMock,
                this.uiState);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        public void SetRestTime_WhenInvalid_DoesNotStartTimer(string input)
        {
            this.systemUnderTest.SetRestTimeCommand.Execute(input);
            this.systemUnderTest.RestTimeRemaining.Should().Be(0);
        }

        [Fact]
        public void StartRestTimer_WhenZeroOrNegative_StopsResting()
        {
            this.systemUnderTest.IsResting = true;
            this.systemUnderTest.StartRestTimer(0);
            this.systemUnderTest.IsResting.Should().BeFalse();
            this.systemUnderTest.RestTimeRemaining.Should().Be(0);
        }

        [Fact]
        public void ApplyTargetGoals_WhenValid_LoadsTemplateAndStartsWorkout()
        {
            this.systemUnderTest.SelectedGoal = "Push";
            var template = new WorkoutTemplate { Id = 1, Name = "Push", Type = WorkoutType.CUSTOM };
            template.AddExercise(new TemplateExercise { Name = "Bench", TargetSets = 3 });
            this.storageMock.GetAvailableWorkouts(1).Returns(new List<WorkoutTemplate> { template });

            this.systemUnderTest.ApplyTargetGoalsCommand.Execute(1);

            this.systemUnderTest.IsWorkoutStarted.Should().BeTrue();
            this.systemUnderTest.WorkoutSessionTitle.Should().Be("Push");
            this.systemUnderTest.ExerciseRows.Should().HaveCount(1);
            this.systemUnderTest.CurrentExerciseName.Should().Be("Bench");
        }

        [Fact]
        public void SaveSet_WhenValid_SavesAndAdvances()
        {
            this.systemUnderTest.IsWorkoutStarted = true;
            var set = new ActiveSetViewModel { ExerciseName = "Squat", SetIndex = 1, ActualReps = 10, ActualWeight = 100 };
            this.clientServiceMock.SaveSet(Arg.Any<WorkoutLog>(), "Squat", Arg.Any<LoggedSet>()).Returns(true);
            
            this.systemUnderTest.ExerciseRows.Add(new ActiveExerciseViewModel(new TemplateExercise { Name = "Squat", TargetSets = 2 }, _ => { }));
            
            this.systemUnderTest.SaveSetCommand.Execute(set);

            set.IsCompleted.Should().BeTrue();
            this.systemUnderTest.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public void SaveSet_WhenServiceFails_SetsErrorMessage()
        {
            this.systemUnderTest.IsWorkoutStarted = true;
            var set = new ActiveSetViewModel { ExerciseName = "Squat", SetIndex = 1, ActualReps = 10, ActualWeight = 100 };
            this.clientServiceMock.SaveSet(Arg.Any<WorkoutLog>(), "Squat", Arg.Any<LoggedSet>()).Returns(false);

            this.systemUnderTest.SaveSetCommand.Execute(set);

            set.IsCompleted.Should().BeFalse();
            this.systemUnderTest.ErrorMessage.Should().Be("Failed to save set. Please try again.");
        }

        [Fact]
        public void FinishWorkout_WhenValid_FinalizesAndNavigates()
        {
            this.systemUnderTest.IsWorkoutStarted = true;
            this.clientServiceMock.FinalizeWorkout(Arg.Any<WorkoutLog>()).Returns(true);

            this.systemUnderTest.FinishWorkoutCommand.Execute(1);

            this.systemUnderTest.IsWorkoutStarted.Should().BeFalse();
            this.navigationMock.Received(1).NavigateToClientDashboard(true);
        }

        [Fact]
        public void RepeatWorkout_WhenHasLastLog_SelectsTemplate()
        {
            this.systemUnderTest.IsWorkoutStarted = true;
            this.clientServiceMock.FinalizeWorkout(Arg.Any<WorkoutLog>()).Returns(true);
            this.systemUnderTest.FinishWorkoutCommand.Execute(1);

            var template = new WorkoutTemplate { Id = 0, Name = "Previous" };
            this.storageMock.GetAvailableWorkouts(1).Returns(new List<WorkoutTemplate> { template });

            this.systemUnderTest.RepeatWorkoutCommand.Execute(1);

            this.systemUnderTest.SelectedTemplate.Should().Be(template);
        }

        [Fact]
        public void LoadNotifications_PopulatesList()
        {
            var notification = new Notification { Id = 1 };
            this.clientServiceMock.GetNotifications(1).Returns(new List<Notification> { notification });

            this.systemUnderTest.LoadNotificationsCommand.Execute(1);

            this.systemUnderTest.Notifications.Should().ContainSingle().Which.Should().Be(notification);
        }

        [Fact]
        public void ConfirmDeload_RemovesNotification()
        {
            var notification = new Notification { Id = 1 };
            this.systemUnderTest.Notifications.Add(notification);

            this.systemUnderTest.ConfirmDeloadCommand.Execute(notification);

            this.clientServiceMock.Received(1).ConfirmDeload(notification);
            this.systemUnderTest.Notifications.Should().BeEmpty();
        }

        [Fact]
        public void CompleteCurrentSet_UpdatesPendingSet()
        {
            this.systemUnderTest.SelectedGoal = "Push";
            var template = new WorkoutTemplate { Id = 1, Name = "Push" };
            template.AddExercise(new TemplateExercise { Name = "Bench", TargetSets = 1 });
            this.storageMock.GetAvailableWorkouts(1).Returns(new List<WorkoutTemplate> { template });
            this.systemUnderTest.ApplyTargetGoalsCommand.Execute(1);

            this.systemUnderTest.CurrentSetRepsInput = 12;
            this.systemUnderTest.CurrentSetWeightInput = 80;

            this.systemUnderTest.CompleteCurrentSetCommand.Execute(null);

            var set = this.systemUnderTest.ExerciseRows[0].Sets[0];
            set.ActualRepsValue.Should().Be(12);
            set.ActualWeightValue.Should().Be(80);
        }

        [Fact]
        public void ActiveSetViewModel_Properties_FunctionCorrectly()
        {
            var autoSaveTriggered = false;
            var set = new ActiveSetViewModel
            {
                AutoSaveHandler = _ => autoSaveTriggered = true
            };

            set.ActualRepsValue = 10;
            set.ActualWeightValue = double.NaN;

            set.ActualReps.Should().Be(10);
            autoSaveTriggered.Should().BeFalse();

            set.ActualWeightValue = 50.5;
            set.ActualWeight.Should().Be(50.5);
            autoSaveTriggered.Should().BeTrue();
        }

        [Fact]
        public void ActiveSetViewModel_WhenNaN_SetsNull()
        {
            var set = new ActiveSetViewModel();
            set.ActualRepsValue = double.NaN;
            set.ActualWeightValue = double.NaN;

            set.ActualReps.Should().BeNull();
            set.ActualWeight.Should().BeNull();
        }
    }
}


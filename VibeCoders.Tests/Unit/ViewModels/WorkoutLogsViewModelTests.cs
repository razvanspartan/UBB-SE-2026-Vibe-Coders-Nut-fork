using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels;

public class WorkoutLogsViewModelTests
{
    private readonly INavigationService mockNavigationService;
    private readonly IClientService mockClientService;
    private readonly WorkoutLogsViewModel workoutLogsViewModel;

    public WorkoutLogsViewModelTests()
    {
        this.mockNavigationService = Substitute.For<INavigationService>();
        this.mockClientService = Substitute.For<IClientService>();
        this.workoutLogsViewModel = new WorkoutLogsViewModel(this.mockNavigationService, this.mockClientService);
    }

    [Fact]
    public void LoadLogs_Should_PopulateLogs_When_ServiceReturnsData()
    {
        int clientIdentifier = 1;
        var sampleWorkoutLogs = new List<WorkoutLog>
        {
            new WorkoutLog { Id = 10, WorkoutName = "Morning Run", Date = DateTime.Now, Exercises = new() }
        };
        this.mockClientService.GetWorkoutHistoryForClient(clientIdentifier).Returns(sampleWorkoutLogs);

        this.workoutLogsViewModel.LoadLogsCommand.Execute(clientIdentifier);

        this.workoutLogsViewModel.Logs.Should().HaveCount(1);
        this.workoutLogsViewModel.Logs[0].WorkoutName.Should().Be("Morning Run");
        this.workoutLogsViewModel.ShowEmptyState.Should().BeFalse();
        this.workoutLogsViewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void LoadLogs_Should_HandleError_When_ServiceThrows()
    {
        this.mockClientService.GetWorkoutHistoryForClient(Arg.Any<int>())
            .Returns(_ => throw new Exception("Database Timeout"));

        this.workoutLogsViewModel.LoadLogsCommand.Execute(1);

        this.workoutLogsViewModel.ErrorMessage.Should().Contain("Database Timeout");
        this.workoutLogsViewModel.ShowEmptyState.Should().BeTrue();
        this.workoutLogsViewModel.Logs.Should().BeEmpty();
    }

    [Fact]
    public void ToggleEditMode_Should_SwitchStateCorrectly()
    {
        var workoutLog = new WorkoutLog { Id = 1, Exercises = new() };
        var workoutLogItemViewModel = new WorkoutLogItemViewModel(workoutLog, this.mockClientService);

        this.workoutLogsViewModel.ToggleEditModeCommand.Execute(workoutLogItemViewModel);
        bool isFirstEditState = workoutLogItemViewModel.IsEditMode;

        this.workoutLogsViewModel.ToggleEditModeCommand.Execute(workoutLogItemViewModel);
        bool isSecondEditState = workoutLogItemViewModel.IsEditMode;

        isFirstEditState.Should().BeTrue();
        isSecondEditState.Should().BeFalse();
    }

    [Fact]
    public void CancelEditMode_Should_RevertChanges_ToOriginalLogData()
    {
        var workoutLog = new WorkoutLog
        {
            Id = 1,
            Exercises = new List<LoggedExercise>
            {
                new LoggedExercise
                {
                    ExerciseName = "Pushups",
                    Sets = new List<LoggedSet> { new LoggedSet { ActualReps = 10 } }
                }
            }
        };
        var workoutLogItemViewModel = new WorkoutLogItemViewModel(workoutLog, this.mockClientService);

        workoutLogItemViewModel.EnterEditMode();
        workoutLogItemViewModel.Exercises[0].Sets[0].Reps = 20;

        workoutLogItemViewModel.CancelEditMode();

        workoutLogItemViewModel.Exercises[0].Sets[0].Reps.Should().Be(10);
        workoutLogItemViewModel.IsEditMode.Should().BeFalse();
    }

    [Theory]
    [InlineData(15.0, 15)]
    [InlineData(15.6, 16)]
    [InlineData(double.NaN, null)]
    public void WorkoutLogSetEditor_RepsInput_Should_MapCorrectIntValues(double userInput, int? expectedRepetitions)
    {
        var workoutLogSetEditorViewModel = new WorkoutLogSetEditorViewModel { SetNumber = 1 };

        workoutLogSetEditorViewModel.RepsInput = userInput;

        workoutLogSetEditorViewModel.Reps.Should().Be(expectedRepetitions);
    }

    [Fact]
    public void ToLoggedExercise_Should_MapViewModelData_BackToModel()
    {
        var loggedExercise = new LoggedExercise { ExerciseName = "Squat", Sets = new() };
        var workoutLogExerciseSummary = new WorkoutLogExerciseSummary(loggedExercise);
        workoutLogExerciseSummary.Sets.Add(new WorkoutLogSetEditorViewModel { Reps = 10, Weight = 100.5 });

        var mappedLoggedExercise = workoutLogExerciseSummary.ToLoggedExercise(99);

        mappedLoggedExercise.WorkoutLogId.Should().Be(99);
        mappedLoggedExercise.ExerciseName.Should().Be("Squat");
        mappedLoggedExercise.Sets.Should().HaveCount(1);
        mappedLoggedExercise.Sets[0].ActualReps.Should().Be(10);
        mappedLoggedExercise.Sets[0].ActualWeight.Should().Be(100.5);
    }
}
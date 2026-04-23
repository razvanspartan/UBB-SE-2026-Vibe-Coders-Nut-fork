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
    private readonly INavigationService mockNavigation;
    private readonly IClientService mockClientService;
    private readonly WorkoutLogsViewModel viewModel;

    public WorkoutLogsViewModelTests()
    {
        this.mockNavigation = Substitute.For<INavigationService>();
        this.mockClientService = Substitute.For<IClientService>();
        this.viewModel = new WorkoutLogsViewModel(this.mockNavigation, this.mockClientService);
    }


    [Fact]
    public void LoadLogs_Should_PopulateLogs_When_ServiceReturnsData()
    {
        int clientId = 1;
        var fakeLogs = new List<WorkoutLog>
        {
            new WorkoutLog { Id = 10, WorkoutName = "Morning Run", Date = DateTime.Now, Exercises = new() }
        };
        this.mockClientService.GetWorkoutHistoryForClient(clientId).Returns(fakeLogs);

        this.viewModel.LoadLogsCommand.Execute(clientId);

        this.viewModel.Logs.Should().HaveCount(1);
        this.viewModel.Logs[0].WorkoutName.Should().Be("Morning Run");
        this.viewModel.ShowEmptyState.Should().BeFalse();
        this.viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void LoadLogs_Should_HandleError_When_ServiceThrows()
    {
        this.mockClientService.GetWorkoutHistoryForClient(Arg.Any<int>())
            .Returns(_ => throw new Exception("Database Timeout"));

        this.viewModel.LoadLogsCommand.Execute(1);

        this.viewModel.ErrorMessage.Should().Contain("Database Timeout");
        this.viewModel.ShowEmptyState.Should().BeTrue();
        this.viewModel.Logs.Should().BeEmpty();
    }

    [Fact]
    public void ToggleEditMode_Should_SwitchStateCorrectly()
    {
        var log = new WorkoutLog { Id = 1, Exercises = new() };
        var itemVm = new WorkoutLogItemViewModel(log, this.mockClientService);

        this.viewModel.ToggleEditModeCommand.Execute(itemVm);
        bool firstState = itemVm.IsEditMode;

        this.viewModel.ToggleEditModeCommand.Execute(itemVm);
        bool secondState = itemVm.IsEditMode;

        firstState.Should().BeTrue();
        secondState.Should().BeFalse();
    }

    [Fact]
    public void CancelEditMode_Should_RevertChanges_ToOriginalLogData()
    {
        var log = new WorkoutLog
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
        var itemVm = new WorkoutLogItemViewModel(log, this.mockClientService);

        itemVm.EnterEditMode();
        itemVm.Exercises[0].Sets[0].Reps = 20;

        itemVm.CancelEditMode();

        itemVm.Exercises[0].Sets[0].Reps.Should().Be(10);
        itemVm.IsEditMode.Should().BeFalse();
    }


    [Theory]
    [InlineData(15.0, 15)]
    [InlineData(15.6, 16)] 
    [InlineData(double.NaN, null)]
    public void WorkoutLogSetEditor_RepsInput_Should_MapCorrectIntValues(double input, int? expectedReps)
    {
        var editor = new WorkoutLogSetEditorViewModel { SetNumber = 1 };

        editor.RepsInput = input;

        editor.Reps.Should().Be(expectedReps);
    }

    [Fact]
    public void ToLoggedExercise_Should_MapViewModelData_BackToModel()
    {
        var exercise = new LoggedExercise { ExerciseName = "Squat", Sets = new() };
        var summary = new WorkoutLogExerciseSummary(exercise);
        summary.Sets.Add(new WorkoutLogSetEditorViewModel { Reps = 10, Weight = 100.5 });

        var result = summary.ToLoggedExercise(99);

        result.WorkoutLogId.Should().Be(99);
        result.ExerciseName.Should().Be("Squat");
        result.Sets.Should().HaveCount(1);
        result.Sets[0].ActualReps.Should().Be(10);
        result.Sets[0].ActualWeight.Should().Be(100.5);
    }
}
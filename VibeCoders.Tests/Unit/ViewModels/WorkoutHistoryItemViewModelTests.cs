using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using NSubstitute;
using VibeCoders.Models.Analytics;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.ViewModels;
using Windows.UI;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels;

public class WorkoutHistoryItemViewModelTests
{
    private readonly IWorkoutAnalyticsStore mockStore;
    private readonly long clientId = 123;

    public WorkoutHistoryItemViewModelTests()
    {
        this.mockStore = Substitute.For<IWorkoutAnalyticsStore>();
    }

    private WorkoutHistoryRow CreateRow(string name = "Full Body", string tag = "Moderate") => new WorkoutHistoryRow
    {
        Id = 1,
        WorkoutName = name,
        LogDate = new DateTime(2026, 4, 23),
        DurationSeconds = 3600,
        TotalCaloriesBurned = 500,
        IntensityTag = tag
    };

    [Fact]
    public void Constructor_Should_HandleEmptyWorkoutName_WithFallback()
    {
       
        var row = this.CreateRow(name: "");

        var viewModel = new WorkoutHistoryItemViewModel(this.mockStore, this.clientId, row);

        viewModel.Title.Should().Be("Workout");
    }
    
    [Theory]
    [InlineData("Light")]
    [InlineData("Moderate")]
    [InlineData("Intense")]
    [InlineData("Unknown")]
    public void IntensityTag_Should_MatchRowData(string expectedTag)
    {
        var row = this.CreateRow(tag: expectedTag);

        var viewModel = new WorkoutHistoryItemViewModel(this.mockStore, this.clientId, row);

        viewModel.IntensityTag.Should().Be(expectedTag);
    }

    [Fact]
    public async Task OnIsExpandedChanged_Should_OnlyLoadDataOnce_WhenExpandedMultipleTimes()
    {
        var row = this.CreateRow();
        var viewModel = new WorkoutHistoryItemViewModel(this.mockStore, this.clientId, row);

        var emptyDetail = new WorkoutSessionDetail
        {
            Sets = Array.Empty<WorkoutSetRow>(),
            ExerciseCalories = Array.Empty<ExerciseCalorieInfo>()
        };

        this.mockStore.GetWorkoutSessionDetailAsync(this.clientId, viewModel.WorkoutLogId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkoutSessionDetail?>(emptyDetail));

        viewModel.IsExpanded = true;
        viewModel.IsExpanded = false;
        viewModel.IsExpanded = true;

        await Task.Delay(100);

        await this.mockStore.Received(1).GetWorkoutSessionDetailAsync(this.clientId, viewModel.WorkoutLogId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadDetailAsync_Should_GroupSetsByExerciseName_And_MaintainOrder()
    {
        var row = this.CreateRow();
        var viewModel = new WorkoutHistoryItemViewModel(this.mockStore, this.clientId, row);

        var detail = new WorkoutSessionDetail
        {
            Sets = new List<WorkoutSetRow>
            {
                new WorkoutSetRow { ExerciseName = "Squat", SetIndex = 0, ActualReps = 10 },
                new WorkoutSetRow { ExerciseName = "Squat", SetIndex = 1, ActualReps = 12 },
                new WorkoutSetRow { ExerciseName = "Bench Press", SetIndex = 0, ActualReps = 8 }
            },
            ExerciseCalories = new List<ExerciseCalorieInfo>
            {
                new ExerciseCalorieInfo { ExerciseName = "Squat", CaloriesBurned = 45 }
            }
        };

        this.mockStore.GetWorkoutSessionDetailAsync(this.clientId, viewModel.WorkoutLogId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkoutSessionDetail?>(detail));

        viewModel.IsExpanded = true;
        await Task.Delay(100);

        viewModel.ExerciseSetGroups.Should().HaveCount(2);

        var squatGroup = viewModel.ExerciseSetGroups.First(g => g.ExerciseName == "Squat");
        squatGroup.Sets.Should().HaveCount(2);
        squatGroup.Sets[0].RepsDisplay.Should().Be("10");

        viewModel.ExerciseCalories.Should().HaveCount(1);
        viewModel.ExerciseCalories[0].CaloriesBurned.Should().Be(45);
    }
}
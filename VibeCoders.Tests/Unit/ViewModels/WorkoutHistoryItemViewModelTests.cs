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
    private readonly IWorkoutAnalyticsStore mockWorkoutAnalyticsStore;
    private readonly long clientIdentifier = 123;

    public WorkoutHistoryItemViewModelTests()
    {
        this.mockWorkoutAnalyticsStore = Substitute.For<IWorkoutAnalyticsStore>();
    }

    private WorkoutHistoryRow CreateWorkoutHistoryRow(string workoutName = "Full Body", string intensityTag = "Moderate") => new WorkoutHistoryRow
    {
        Id = 1,
        WorkoutName = workoutName,
        LogDate = new DateTime(2026, 4, 23),
        DurationSeconds = 3600,
        TotalCaloriesBurned = 500,
        IntensityTag = intensityTag
    };

    [Fact]
    public void Constructor_Should_HandleEmptyWorkoutName_WithFallback()
    {
        var workoutHistoryRow = this.CreateWorkoutHistoryRow(workoutName: "");

        var workoutHistoryItemViewModel = new WorkoutHistoryItemViewModel(this.mockWorkoutAnalyticsStore, this.clientIdentifier, workoutHistoryRow);

        workoutHistoryItemViewModel.Title.Should().Be("Workout");
    }

    [Theory]
    [InlineData("Light")]
    [InlineData("Moderate")]
    [InlineData("Intense")]
    [InlineData("Unknown")]
    public void IntensityTag_Should_MatchRowData(string expectedIntensityTag)
    {
        var workoutHistoryRow = this.CreateWorkoutHistoryRow(intensityTag: expectedIntensityTag);

        var workoutHistoryItemViewModel = new WorkoutHistoryItemViewModel(this.mockWorkoutAnalyticsStore, this.clientIdentifier, workoutHistoryRow);

        workoutHistoryItemViewModel.IntensityTag.Should().Be(expectedIntensityTag);
    }

    [Fact]
    public async Task OnIsExpandedChanged_Should_OnlyLoadDataOnce_WhenExpandedMultipleTimes()
    {
        var workoutHistoryRow = this.CreateWorkoutHistoryRow();
        var workoutHistoryItemViewModel = new WorkoutHistoryItemViewModel(this.mockWorkoutAnalyticsStore, this.clientIdentifier, workoutHistoryRow);

        var emptyWorkoutSessionDetail = new WorkoutSessionDetail
        {
            Sets = Array.Empty<WorkoutSetRow>(),
            ExerciseCalories = Array.Empty<ExerciseCalorieInfo>()
        };

        this.mockWorkoutAnalyticsStore.GetWorkoutSessionDetailAsync(this.clientIdentifier, workoutHistoryItemViewModel.WorkoutLogId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkoutSessionDetail?>(emptyWorkoutSessionDetail));

        workoutHistoryItemViewModel.IsExpanded = true;
        workoutHistoryItemViewModel.IsExpanded = false;
        workoutHistoryItemViewModel.IsExpanded = true;

        await Task.Delay(100);

        await this.mockWorkoutAnalyticsStore.Received(1).GetWorkoutSessionDetailAsync(this.clientIdentifier, workoutHistoryItemViewModel.WorkoutLogId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadDetailAsync_Should_GroupSetsByExerciseName_And_MaintainOrder()
    {
        var workoutHistoryRow = this.CreateWorkoutHistoryRow();
        var workoutHistoryItemViewModel = new WorkoutHistoryItemViewModel(this.mockWorkoutAnalyticsStore, this.clientIdentifier, workoutHistoryRow);

        var workoutSessionDetail = new WorkoutSessionDetail
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

        this.mockWorkoutAnalyticsStore.GetWorkoutSessionDetailAsync(this.clientIdentifier, workoutHistoryItemViewModel.WorkoutLogId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkoutSessionDetail?>(workoutSessionDetail));

        workoutHistoryItemViewModel.IsExpanded = true;
        await Task.Delay(100);

        workoutHistoryItemViewModel.ExerciseSetGroups.Should().HaveCount(2);

        var exerciseGroup = workoutHistoryItemViewModel.ExerciseSetGroups.First(group => group.ExerciseName == "Squat");
        exerciseGroup.Sets.Should().HaveCount(2);

        exerciseGroup.Sets[0].RepsDisplay.Should().Be("10");

        workoutHistoryItemViewModel.ExerciseCalories.Should().HaveCount(1);
        workoutHistoryItemViewModel.ExerciseCalories[0].CaloriesBurned.Should().Be(45);
    }

}
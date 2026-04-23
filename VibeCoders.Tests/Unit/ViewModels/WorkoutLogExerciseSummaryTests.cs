using FluentAssertions;
using static VibeCoders.Tests.Mocks.DataFactories.WorkoutLogViewModelDataFactory;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels;

public sealed class WorkoutLogExerciseSummaryTests
{
    private const int WorkoutLogIdentifier = 25;
    private const int ExpectedSetCount = 2;

    [Fact]
    public void Constructor_WhenAdjustmentNoteExists_UsesAdjustmentNoteAndOrdersSetsBySetIndex()
    {
        var loggedExercise = WorkoutLogViewModelDataFactory.CreateLoggedExerciseWithAdjustmentNote();

        var workoutLogExerciseSummary = new WorkoutLogExerciseSummary(loggedExercise);

        workoutLogExerciseSummary.TooltipText.Should().Be("System adjusted after last workout.");
        workoutLogExerciseSummary.Sets.Should().HaveCount(ExpectedSetCount);
        workoutLogExerciseSummary.Sets[0].SetNumber.Should().Be(1);
        workoutLogExerciseSummary.Sets[0].Reps.Should().Be(10);
        workoutLogExerciseSummary.Sets[1].SetNumber.Should().Be(2);
        workoutLogExerciseSummary.Sets[1].Reps.Should().Be(8);
    }

    [Fact]
    public void ToLoggedExercise_WhenSummaryContainsEditedSets_RenumbersSetsSequentially()
    {
        var loggedExercise = WorkoutLogViewModelDataFactory.CreateLoggedExerciseWithoutAdjustmentNote();
        var workoutLogExerciseSummary = new WorkoutLogExerciseSummary(loggedExercise);

        var updatedLoggedExercise = workoutLogExerciseSummary.ToLoggedExercise(WorkoutLogIdentifier);

        updatedLoggedExercise.WorkoutLogId.Should().Be(WorkoutLogIdentifier);
        updatedLoggedExercise.ExerciseName.Should().Be("Deadlift");
        updatedLoggedExercise.Sets.Should().HaveCount(ExpectedSetCount);
        updatedLoggedExercise.Sets[0].SetIndex.Should().Be(1);
        updatedLoggedExercise.Sets[0].SetNumber.Should().Be(1);
        updatedLoggedExercise.Sets[1].SetIndex.Should().Be(2);
        updatedLoggedExercise.Sets[1].SetNumber.Should().Be(2);
    }
}
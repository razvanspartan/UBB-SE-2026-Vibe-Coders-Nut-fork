using FluentAssertions;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels;

public sealed class WorkoutLogSetEditorViewModelTests
{
    private const int RoundedRepetitions = 6;
    private const string ExpectedWeightDisplay = "62.5 kg";

    [Fact]
    public void RepsInput_WhenValueIsRounded_UpdatesRepsAndDisplay()
    {
        var workoutLogSetEditorViewModel = new WorkoutLogSetEditorViewModel();

        workoutLogSetEditorViewModel.RepsInput = 5.6;

        workoutLogSetEditorViewModel.Reps.Should().Be(RoundedRepetitions);
        workoutLogSetEditorViewModel.RepsDisplay.Should().Be("6");
    }

    [Fact]
    public void WeightInput_WhenValueExists_FormatsWeightDisplayWithKilograms()
    {
        var workoutLogSetEditorViewModel = new WorkoutLogSetEditorViewModel();

        workoutLogSetEditorViewModel.WeightInput = 62.5;

        workoutLogSetEditorViewModel.Weight.Should().Be(62.5);
        workoutLogSetEditorViewModel.WeightDisplay.Should().Be(ExpectedWeightDisplay);
    }
}
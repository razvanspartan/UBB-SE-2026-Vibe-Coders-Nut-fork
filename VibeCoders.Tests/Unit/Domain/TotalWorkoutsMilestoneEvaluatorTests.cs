using System;
using System.Collections.Generic;
using FluentAssertions;
using VibeCoders.Domain;
using Xunit;

namespace VibeCoders.Tests.Unit.Domain
{
    public class TotalWorkoutsMilestoneEvaluatorTests
    {
        private const int WorkoutCount = 25;
        private const int PreviousWorkoutCount = 9;
        private const int CurrentWorkoutCount = 25;
        private const int NegativeWorkoutCount = -1;

        private readonly TotalWorkoutsMilestoneEvaluator systemUnderTest = new();

        private static readonly IReadOnlyList<WorkoutMilestone> ExpectedEarnedMilestones =
        [
            new WorkoutMilestone(1, "First Rep", "Complete your first workout."),
            new WorkoutMilestone(10, "Getting Serious", "Complete 10 total workouts."),
            new WorkoutMilestone(25, "Gym Regular", "Complete 25 total workouts."),
        ];

        private static readonly IReadOnlyList<WorkoutMilestone> ExpectedNewlyEarnedMilestones =
        [
            new WorkoutMilestone(10, "Getting Serious", "Complete 10 total workouts."),
            new WorkoutMilestone(25, "Gym Regular", "Complete 25 total workouts."),
        ];

        [Fact]
        public void GetEarnedMilestones_WhenWorkoutCountMeetsThresholds_ReturnsEarnedMilestonesInOrder()
        {
            var earnedMilestones = this.systemUnderTest.GetEarnedMilestones(WorkoutCount);

            earnedMilestones.Should().Equal(ExpectedEarnedMilestones);
        }

        [Fact]
        public void GetNewlyEarnedMilestones_WhenWorkoutCountCrossesThresholds_ReturnsOnlyNewMilestones()
        {
            var newlyEarnedMilestones = this.systemUnderTest.GetNewlyEarnedMilestones(PreviousWorkoutCount, CurrentWorkoutCount);

            newlyEarnedMilestones.Should().Equal(ExpectedNewlyEarnedMilestones);
        }

        [Fact]
        public void GetEarnedMilestones_WhenWorkoutCountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Action getEarnedMilestones = () => this.systemUnderTest.GetEarnedMilestones(NegativeWorkoutCount);

            getEarnedMilestones.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
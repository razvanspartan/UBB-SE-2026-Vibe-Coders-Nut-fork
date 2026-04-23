using FluentAssertions;
using NSubstitute;
using VibeCoders.Domain;
using VibeCoders.Repositories.Interfaces;
using Xunit;

namespace VibeCoders.Tests.Unit.Domain
{
    public class MilestoneCheckTests
    {
        private const int DefaultClientId = 1;
        private const int OneWorkout = 1;
        private const int CurrentStreak = 2;
        private const int RequiredStreak = 3;
        private const int RequiredWeeklyWorkouts = 4;
        private const int RequiredTotalWorkouts = 5;

        private readonly IRepositoryAchievements achievementsRepositoryMock = Substitute.For<IRepositoryAchievements>();

        [Fact]
        public void IsMet_WhenFirstWorkoutExists_ReturnsTrue()
        {
            var firstWorkoutCheck = new FirstWorkoutCheck();
            this.achievementsRepositoryMock.GetWorkoutCount(DefaultClientId).Returns(OneWorkout);

            var milestoneIsMet = firstWorkoutCheck.IsMet(DefaultClientId, this.achievementsRepositoryMock);

            milestoneIsMet.Should().BeTrue();
        }

        [Fact]
        public void IsMet_WhenStreakDoesNotMeetRequiredDays_ReturnsFalse()
        {
            var streakCheck = new StreakCheck("Consistency", RequiredStreak);
            this.achievementsRepositoryMock.GetConsecutiveWorkoutDayStreak(DefaultClientId).Returns(CurrentStreak);

            var milestoneIsMet = streakCheck.IsMet(DefaultClientId, this.achievementsRepositoryMock);

            milestoneIsMet.Should().BeFalse();
        }

        [Fact]
        public void IsMet_WhenWeeklyWorkoutCountMeetsRequiredVolume_ReturnsTrue()
        {
            var weeklyVolumeCheck = new WeeklyVolumeCheck("Momentum", RequiredWeeklyWorkouts);
            this.achievementsRepositoryMock.GetWorkoutsInLastSevenDays(DefaultClientId).Returns(RequiredWeeklyWorkouts);

            var milestoneIsMet = weeklyVolumeCheck.IsMet(DefaultClientId, this.achievementsRepositoryMock);

            milestoneIsMet.Should().BeTrue();
        }

        [Fact]
        public void IsMet_WhenWorkoutCountMeetsThreshold_ReturnsTrue()
        {
            var workoutCountCheck = new WorkoutCountCheck("Committed", RequiredTotalWorkouts);
            this.achievementsRepositoryMock.GetWorkoutCount(DefaultClientId).Returns(RequiredTotalWorkouts);

            var milestoneIsMet = workoutCountCheck.IsMet(DefaultClientId, this.achievementsRepositoryMock);

            milestoneIsMet.Should().BeTrue();
        }
    }
}
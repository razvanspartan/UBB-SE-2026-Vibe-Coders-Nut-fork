using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using VibeCoders.Tests.Mocks.DataFactories;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class WorkoutDataForwarderTests
    {
        private readonly IWorkoutAnalyticsStore mockWorkoutAnalyticsStore = Substitute.For<IWorkoutAnalyticsStore>();
        private readonly IAnalyticsDashboardRefreshBus mockAnalyticsDashboardRefreshBus = Substitute.For<IAnalyticsDashboardRefreshBus>();
        private readonly WorkoutDataForwarder workoutDataForwarder;
        private readonly int mockIdToReturn = 100;
        private readonly int caloriesBurnedLowIntensity = 150;
        private readonly int caloriesBurnedHighIntensity = 700;
        private readonly int caloriesBurnedModerateIntensity = 350; 
        private readonly float averageMetabolicEquivalentLowIntensity = 2.5f;
        private readonly float averageMetabolicEquivalentModerateIntensity = 5.5f;
        private readonly float averageMetabolicEquivalentHighIntensity = 8.75f;
        public WorkoutDataForwarderTests()
        {
            this.mockWorkoutAnalyticsStore = Substitute.For<IWorkoutAnalyticsStore>();
            this.mockAnalyticsDashboardRefreshBus = Substitute.For<IAnalyticsDashboardRefreshBus>();
            this.workoutDataForwarder = new WorkoutDataForwarder(this.mockWorkoutAnalyticsStore, this.mockAnalyticsDashboardRefreshBus);
            this.mockWorkoutAnalyticsStore.SaveWorkoutAsync(Arg.Any<long>(), Arg.Any<WorkoutLog>(), Arg.Any<CancellationToken>())
                  .Returns(this.mockIdToReturn);
        }
        [Fact]
        public async Task ForwardCompletedWorkoutAsync_Should_SaveCorrectDataToStore_LowIntensity()
        {
            var mockedLowIntensityWorkoutLog = WorkoutLogFactory.CreateLowIntensityWorkoutLog();

            int resultId = await this.workoutDataForwarder.ForwardCompletedWorkoutAsync(1, mockedLowIntensityWorkoutLog);
            mockedLowIntensityWorkoutLog.TotalCaloriesBurned.Should().Be(this.caloriesBurnedLowIntensity);
            mockedLowIntensityWorkoutLog.AverageMetabolicEquivalent.Should().Be(this.averageMetabolicEquivalentLowIntensity);
            mockedLowIntensityWorkoutLog.IntensityTag.Should().Be("light");
            resultId.Should().Be(this.mockIdToReturn);
        }
        [Fact]
        public async Task ForwardCompletedWorkoutAsync_Should_SaveCorrectDataToStore_ModerateIntensity()
        {
            var mockedModerateIntensityWorkoutLog = WorkoutLogFactory.CreateModerateIntensityWorkoutLog();

            int resultId = await this.workoutDataForwarder.ForwardCompletedWorkoutAsync(1, mockedModerateIntensityWorkoutLog);
            mockedModerateIntensityWorkoutLog.TotalCaloriesBurned.Should().Be(this.caloriesBurnedModerateIntensity);
            mockedModerateIntensityWorkoutLog.AverageMetabolicEquivalent.Should().Be(this.averageMetabolicEquivalentModerateIntensity);
            mockedModerateIntensityWorkoutLog.IntensityTag.Should().Be("moderate");
            resultId.Should().Be(this.mockIdToReturn);
        }
        [Fact]
        public async Task ForwardCompletedWorkoutAsync_Should_SaveCorrectDataToStore_HighIntensity()
        {
            var mockedHighIntensityWorkoutLog = WorkoutLogFactory.CreateHighIntensityWorkoutLog();

            int resultId = await this.workoutDataForwarder.ForwardCompletedWorkoutAsync(1, mockedHighIntensityWorkoutLog);
            mockedHighIntensityWorkoutLog.TotalCaloriesBurned.Should().Be(this.caloriesBurnedHighIntensity);
            mockedHighIntensityWorkoutLog.AverageMetabolicEquivalent.Should().Be(this.averageMetabolicEquivalentHighIntensity);
            mockedHighIntensityWorkoutLog.IntensityTag.Should().Be("intense");
            resultId.Should().Be(this.mockIdToReturn);
        }
    }
}

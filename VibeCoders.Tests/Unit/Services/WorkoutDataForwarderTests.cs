using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
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
        public WorkoutDataForwarderTests()
        {
            this.mockWorkoutAnalyticsStore = Substitute.For<IWorkoutAnalyticsStore>();
            this.mockAnalyticsDashboardRefreshBus = Substitute.For<IAnalyticsDashboardRefreshBus>();
            this.workoutDataForwarder = new WorkoutDataForwarder(this.mockWorkoutAnalyticsStore, this.mockAnalyticsDashboardRefreshBus);
            this.mockWorkoutAnalyticsStore.SaveWorkoutAsync(Arg.Any<long>(), Arg.Any<WorkoutLog>(), Arg.Any<CancellationToken>())
                  .Returns(this.mockIdToReturn);
        }
        [Fact]
        public async Task ForwardCompletedWorkoutAsync_Should_SaveCorrectDataToStore()
        {
            var mockedLowIntensityWorkoutLog = WorkoutLogFactory.CreateLowIntensityWorkoutLog();

            int resultId = await this.workoutDataForwarder.ForwardCompletedWorkoutAsync(1, mockedLowIntensityWorkoutLog);

            mockedLowIntensityWorkoutLog.TotalCaloriesBurned.Should().Be(this.caloriesBurnedLowIntensity);
            mockedLowIntensityWorkoutLog.IntensityTag.Should().Be("light");
            resultId.Should().Be(this.mockIdToReturn);
        }
    }
}

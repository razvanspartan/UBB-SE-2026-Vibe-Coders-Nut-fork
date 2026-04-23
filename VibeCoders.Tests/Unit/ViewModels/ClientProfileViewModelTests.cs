using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class ClientProfileViewModelTests
    {
        private const int DefaultClientId = 1;
        private const int WorkoutLogId = 10;
        private const int ActiveNutritionPlanId = 5;
        private static readonly DateTime LatestWorkoutDate = new (2023, 1, 1, 10, 0, 0);

        private readonly IClientService clientServiceMock;
        private readonly ClientProfileViewModel systemUnderTest;

        private const string SyncFailedMessage = "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";

        public ClientProfileViewModelTests()
        {
            this.clientServiceMock = Substitute.For<IClientService>();
            this.systemUnderTest = new ClientProfileViewModel(this.clientServiceMock);
        }

        [Fact]
        public async Task SyncNutritionCommand_NoClientId_DoesNothing()
        {
            await this.systemUnderTest.SyncNutritionCommand.ExecuteAsync(null);

            this.systemUnderTest.SyncNutritionStatus.Should().BeEmpty();
        }

        [Fact]
        public void LoadClientData_ValidClientId_UpdatesPropertiesCorrectly()
        {
            var loggedExercises = new List<LoggedExercise>
            {
                new LoggedExercise { ExerciseName = "Bench Press" }
            };

            var meals = new List<Meal>
            {
                new Meal { Name = "Chicken Rice" }
            };

            var clientProfileSnapshot = new ClientService.ClientProfileSnapshot
            {
                CaloriesSummary = "Calories burned (all logged workouts): 500",
                LatestSessionHint = $"Latest session: Chest Day — {LatestWorkoutDate:g}",
                LoggedExercises = loggedExercises,
                Meals = meals,
            };

            this.clientServiceMock.BuildClientProfileSnapshot(DefaultClientId).Returns(clientProfileSnapshot);

            this.systemUnderTest.LoadClientData(DefaultClientId);

            this.systemUnderTest.CaloriesSummary.Should().Be("Calories burned (all logged workouts): 500");
            this.systemUnderTest.LatestSessionHint.Should().Be($"Latest session: Chest Day — {LatestWorkoutDate:g}");
            this.systemUnderTest.LoggedExercises.Should().HaveCount(1);
            this.systemUnderTest.LoggedExercises[0].ExerciseName.Should().Be("Bench Press");
            this.systemUnderTest.Meals.Should().HaveCount(1);
            this.systemUnderTest.Meals[0].Name.Should().Be("Chicken Rice");
        }

        [Fact]
        public void LoadClientData_NoHistory_UpdatesPropertiesToEmptyState()
        {
            var emptyClientProfileSnapshot = new ClientService.ClientProfileSnapshot();

            this.clientServiceMock.BuildClientProfileSnapshot(DefaultClientId).Returns(emptyClientProfileSnapshot);

            this.systemUnderTest.LoadClientData(DefaultClientId);

            this.systemUnderTest.CaloriesSummary.Should().Be("Calories burned (all logged workouts): 0");
            this.systemUnderTest.LatestSessionHint.Should().BeEmpty();
            this.systemUnderTest.LoggedExercises.Should().BeEmpty();
            this.systemUnderTest.Meals.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncNutritionCommand_SyncFails_UpdatesStatusCorrectly()
        {
            var nutritionSyncPayload = new NutritionSyncPayload();
            var emptyClientProfileSnapshot = new ClientService.ClientProfileSnapshot();

            this.clientServiceMock.BuildClientProfileSnapshot(DefaultClientId).Returns(emptyClientProfileSnapshot);
            this.clientServiceMock.BuildNutritionSyncPayload(DefaultClientId).Returns(nutritionSyncPayload);
            this.clientServiceMock.SyncNutritionAsync(nutritionSyncPayload, Arg.Any<CancellationToken>()).Returns(false);

            this.systemUnderTest.LoadClientData(DefaultClientId);

            await this.systemUnderTest.SyncNutritionCommand.ExecuteAsync(null);

            this.systemUnderTest.SyncNutritionStatus.Should().Be(SyncFailedMessage);
        }
    }
}
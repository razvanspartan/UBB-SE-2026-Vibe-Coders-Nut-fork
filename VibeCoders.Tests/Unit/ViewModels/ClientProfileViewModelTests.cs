using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class ClientProfileViewModelTests
    {
        private readonly IRepositoryWorkoutLog _workoutLogRepo;
        private readonly IRepositoryTrainer _trainerRepo;
        private readonly IRepositoryNutrition _nutritionRepo;
        private readonly ProgressionService _progressionService;
        private readonly EvaluationEngine _evaluationEngine;
        private readonly ClientService _clientService;
        private readonly ClientProfileViewModel _sut;

        public ClientProfileViewModelTests()
        {
            _workoutLogRepo = Substitute.For<IRepositoryWorkoutLog>();
            _trainerRepo = Substitute.For<IRepositoryTrainer>();
            _nutritionRepo = Substitute.For<IRepositoryNutrition>();
            _progressionService = new ProgressionService(Substitute.For<IRepositoryWorkoutTemplate>(), Substitute.For<IRepositoryNotification>());
            _evaluationEngine = new EvaluationEngine(Substitute.For<IRepositoryAchievements>());

            // ClientService is a concrete class but its methods are essentially dependent on the repositories
            _clientService = new ClientService(
                _workoutLogRepo,
                _progressionService,
                Substitute.For<IHttpClientFactory>(),
                _evaluationEngine,
                Substitute.For<IAchievementUnlockedBus>(),
                new NutritionSyncOptions { Endpoint = "http://localhost/sync" },
                _trainerRepo,
                Substitute.For<IRepositoryNotification>(),
                Substitute.For<IRepositoryAchievements>(),
                _nutritionRepo,
                Substitute.For<IRepositoryWorkoutTemplate>()
            );

            _sut = new ClientProfileViewModel(_clientService);
        }

        [Fact]
        public async Task SyncNutritionCommand_NoClientId_DoesNothing()
        {
            // Act
            await _sut.SyncNutritionCommand.ExecuteAsync(null);

            // Assert
            _sut.SyncNutritionStatus.Should().BeEmpty();
        }

        [Fact]
        public void LoadClientData_ValidClientId_UpdatesPropertiesCorrectly()
        {
            // Arrange
            int clientId = 1;

            var history = new List<WorkoutLog>
            {
                new WorkoutLog
                {
                    Id = 10,
                    WorkoutName = "Chest Day",
                    Date = new DateTime(2023, 1, 1, 10, 0, 0),
                    TotalCaloriesBurned = 500,
                    Exercises = new List<LoggedExercise>
                    {
                        new LoggedExercise { ExerciseName = "Bench Press" }
                    }
                }
            };

            _workoutLogRepo.GetWorkoutHistory(clientId).Returns(history);

            var activePlan = new NutritionPlan { PlanId = 5, StartDate = DateTime.Today.AddDays(-1), EndDate = DateTime.Today.AddDays(1) };
            _nutritionRepo.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan> { activePlan });

            var meals = new List<Meal>
            {
                new Meal { Name = "Chicken Rice" }
            };
            _nutritionRepo.GetMealsForPlan(activePlan.PlanId).Returns(meals);

            // Act
            _sut.LoadClientData(clientId);

            // Assert
            _sut.CaloriesSummary.Should().Be("Calories burned (all logged workouts): 500");
            _sut.LatestSessionHint.Should().Be($"Latest session: Chest Day — {new DateTime(2023, 1, 1, 10, 0, 0):g}");
            _sut.LoggedExercises.Should().HaveCount(1);
            _sut.LoggedExercises[0].ExerciseName.Should().Be("Bench Press");
            _sut.Meals.Should().HaveCount(1);
            _sut.Meals[0].Name.Should().Be("Chicken Rice");
        }

        [Fact]
        public void LoadClientData_NoHistory_UpdatesPropertiesToEmptyState()
        {
            // Arrange
            int clientId = 1;
            _workoutLogRepo.GetWorkoutHistory(clientId).Returns(new List<WorkoutLog>());
            _nutritionRepo.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan>());

            // Act
            _sut.LoadClientData(clientId);

            // Assert
            _sut.CaloriesSummary.Should().Be("Calories burned (all logged workouts): 0");
            _sut.LatestSessionHint.Should().Be("No completed workouts with exercises yet.");
            _sut.LoggedExercises.Should().BeEmpty();
            _sut.Meals.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncNutritionCommand_SyncFails_UpdatesStatusCorrectly()
        {
            // Arrange
            int clientId = 1;

            _workoutLogRepo.GetWorkoutHistory(clientId).Returns(new List<WorkoutLog>());
            _trainerRepo.GetTrainerClients(Arg.Any<int>()).Returns(x => throw new Exception("DB Error"));
            _nutritionRepo.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan>());

            _sut.LoadClientData(clientId); // Sets loadedClientId

            // Act
            await _sut.SyncNutritionCommand.ExecuteAsync(null);

            // Assert
            _sut.SyncNutritionStatus.Should().Be("Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.");
        }
    }
}
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
        private readonly IRepositoryWorkoutLog workoutLogRepo;
        private readonly IRepositoryTrainer trainerRepo;
        private readonly IRepositoryNutrition nutritionRepo;
        private readonly ProgressionService progressionService;
        private readonly EvaluationEngine evaluationEngine;
        private readonly ClientService clientService;
        private readonly ClientProfileViewModel sut;

        private const string SyncFailedMessage = "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";

        public ClientProfileViewModelTests()
        {
            this.workoutLogRepo = Substitute.For<IRepositoryWorkoutLog>();
            this.trainerRepo = Substitute.For<IRepositoryTrainer>();
            this.nutritionRepo = Substitute.For<IRepositoryNutrition>();
            this.progressionService = new ProgressionService(Substitute.For<IRepositoryWorkoutTemplate>(), Substitute.For<IRepositoryNotification>());
            this.evaluationEngine = new EvaluationEngine(Substitute.For<IRepositoryAchievements>());

            this.clientService = new ClientService(
                this.workoutLogRepo,
                this.progressionService,
                Substitute.For<IHttpClientFactory>(),
                this.evaluationEngine,
                Substitute.For<IAchievementUnlockedBus>(),
                new NutritionSyncOptions { Endpoint = "http://localhost/sync" },
                this.trainerRepo,
                Substitute.For<IRepositoryNotification>(),
                Substitute.For<IRepositoryAchievements>(),
                this.nutritionRepo,
                Substitute.For<IRepositoryWorkoutTemplate>()
            );

            this.sut = new ClientProfileViewModel(this.clientService);
        }

        [Fact]
        public async Task SyncNutritionCommand_NoClientId_DoesNothing()
        {
            await this.sut.SyncNutritionCommand.ExecuteAsync(null);

            this.sut.SyncNutritionStatus.Should().BeEmpty();
        }

        [Fact]
        public void LoadClientData_ValidClientId_UpdatesPropertiesCorrectly()
        {
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

            this.workoutLogRepo.GetWorkoutHistory(clientId).Returns(history);

            var activePlan = new NutritionPlan { PlanId = 5, StartDate = DateTime.Today.AddDays(-1), EndDate = DateTime.Today.AddDays(1) };
            this.nutritionRepo.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan> { activePlan });

            var meals = new List<Meal>
            {
                new Meal { Name = "Chicken Rice" }
            };
            this.nutritionRepo.GetMealsForPlan(activePlan.PlanId).Returns(meals);

            this.sut.LoadClientData(clientId);

            this.sut.CaloriesSummary.Should().Be("Calories burned (all logged workouts): 500");
            this.sut.LatestSessionHint.Should().Be($"Latest session: Chest Day — {new DateTime(2023, 1, 1, 10, 0, 0):g}");
            this.sut.LoggedExercises.Should().HaveCount(1);
            this.sut.LoggedExercises[0].ExerciseName.Should().Be("Bench Press");
            this.sut.Meals.Should().HaveCount(1);
            this.sut.Meals[0].Name.Should().Be("Chicken Rice");
        }

        [Fact]
        public void LoadClientData_NoHistory_UpdatesPropertiesToEmptyState()
        {
            int clientId = 1;
            this.workoutLogRepo.GetWorkoutHistory(clientId).Returns(new List<WorkoutLog>());
            this.nutritionRepo.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan>());

            this.sut.LoadClientData(clientId);

            this.sut.CaloriesSummary.Should().Be("Calories burned (all logged workouts): 0");
            this.sut.LatestSessionHint.Should().Be("No completed workouts with exercises yet.");
            this.sut.LoggedExercises.Should().BeEmpty();
            this.sut.Meals.Should().BeEmpty();
        }

        [Fact]
        public async Task SyncNutritionCommand_SyncFails_UpdatesStatusCorrectly()
        {
            int clientId = 1;

            this.workoutLogRepo.GetWorkoutHistory(clientId).Returns(new List<WorkoutLog>());
            this.trainerRepo.GetTrainerClients(Arg.Any<int>()).Returns(x => throw new Exception("DB Error"));
            this.nutritionRepo.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan>());

            this.sut.LoadClientData(clientId); 

            await this.sut.SyncNutritionCommand.ExecuteAsync(null);

            this.sut.SyncNutritionStatus.Should().Be(SyncFailedMessage);
        }
    }
}
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using VibeCoders.Tests.Mocks.DataFactories;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class ClientServiceTests
    {
        private const int ValidClientIdentifier = 1;
        private const int InvalidClientIdentifier = 0;
        private const int EmptyCollectionCount = 0;
        private const int ExpectedMealsCount = 1;
        private const int OneMethodCall = 1;
        private const double DefaultClientWeightInKilograms = 80;
        private const double DefaultClientHeightInMeters = 1.8;
        private const int DefaultCaloriesBurned = 500;
        private const int SnapshotCaloriesBurned = 300;
        private const int DefaultPlanIdentifier = 1;
        private const int OneDayDifference = 1;
        private const string DatabaseErrorMessage = "Database Error";
        private const string NutritionSyncEndpointUrl = "http://localhost/sync";
        private const string SquatExerciseName = "Squat";
        private const float SquatMetabolicEquivalent = 5.0f;
        private const int SixtyMinutesInSeconds = 60;
        private const string EmptyDurationDisplay = "00:00";
        private const int FirstSetIndex = 1;
        private const string HighIntensityTag = "High";
        private const string LegDayWorkoutName = "Leg Day";

        private readonly IRepositoryWorkoutLog workoutLogRepository;
        private readonly ProgressionService progressionService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly EvaluationEngine evaluationEngine;
        private readonly IAchievementUnlockedBus achievementBus;
        private readonly NutritionSyncOptions nutritionSync;
        private readonly IRepositoryTrainer trainerRepository;
        private readonly IRepositoryNotification notificationRepository;
        private readonly IRepositoryAchievements achievementsRepository;
        private readonly IRepositoryNutrition nutritionRepository;
        private readonly IRepositoryWorkoutTemplate workoutTemplateRepository;
        private readonly ClientService systemUnderTest;

        public ClientServiceTests()
        {
            this.workoutLogRepository = Substitute.For<IRepositoryWorkoutLog>();
            this.progressionService = new ProgressionService(Substitute.For<IRepositoryWorkoutTemplate>(), Substitute.For<IRepositoryNotification>());
            this.httpClientFactory = Substitute.For<IHttpClientFactory>();
            this.evaluationEngine = new EvaluationEngine(Substitute.For<IRepositoryAchievements>());
            this.achievementBus = Substitute.For<IAchievementUnlockedBus>();
            this.nutritionSync = new NutritionSyncOptions { Endpoint = NutritionSyncEndpointUrl };
            this.trainerRepository = Substitute.For<IRepositoryTrainer>();
            this.notificationRepository = Substitute.For<IRepositoryNotification>();
            this.achievementsRepository = Substitute.For<IRepositoryAchievements>();
            this.nutritionRepository = Substitute.For<IRepositoryNutrition>();
            this.workoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();

            this.systemUnderTest = new ClientService(
                this.workoutLogRepository,
                this.progressionService,
                this.httpClientFactory,
                this.evaluationEngine,
                this.achievementBus,
                this.nutritionSync,
                this.trainerRepository,
                this.notificationRepository,
                this.achievementsRepository,
                this.nutritionRepository,
                this.workoutTemplateRepository);
        }

        [Fact]
        public void GetWorkoutHistoryForClient_ValidClientId_ReturnsWorkoutLogs()
        {
            var clientId = ValidClientIdentifier;
            var expectedLogs = new List<WorkoutLog> { WorkoutLogFactory.CreateEmptyWorkoutLog(clientId) };
            this.workoutLogRepository.GetWorkoutHistory(clientId).Returns(expectedLogs);

            var result = this.systemUnderTest.GetWorkoutHistoryForClient(clientId);

            result.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public void GetWorkoutHistoryForClient_RepositoryThrowsException_ReturnsEmptyList()
        {
            var clientId = ValidClientIdentifier;
            this.workoutLogRepository.When(x => x.GetWorkoutHistory(clientId)).Do(x => { throw new Exception(DatabaseErrorMessage); });

            var result = this.systemUnderTest.GetWorkoutHistoryForClient(clientId);

            result.Should().BeEmpty();
        }

        [Fact]
        public void UpdateWorkoutLog_ValidLog_ReturnsTrue()
        {
            var log = WorkoutLogFactory.CreateEmptyWorkoutLog();
            this.workoutLogRepository.UpdateWorkoutLog(log).Returns(true);

            var result = this.systemUnderTest.UpdateWorkoutLog(log);

            result.Should().BeTrue();
        }

        [Fact]
        public void UpdateWorkoutLog_RepositoryThrowsException_ReturnsFalse()
        {
            var log = WorkoutLogFactory.CreateEmptyWorkoutLog();
            this.workoutLogRepository.When(x => x.UpdateWorkoutLog(log)).Do(x => { throw new Exception(DatabaseErrorMessage); });

            var result = this.systemUnderTest.UpdateWorkoutLog(log);

            result.Should().BeFalse();
        }

        [Fact]
        public void FinalizeWorkout_NullLog_ReturnsFalse()
        {
            var result = this.systemUnderTest.FinalizeWorkout(null!);

            result.Should().BeFalse();
        }

        [Fact]
        public void FinalizeWorkout_ValidLog_ReturnsTrue()
        {
            var log = new WorkoutLog
            {
                Id = ValidClientIdentifier,
                ClientId = ValidClientIdentifier,
                Exercises = new List<LoggedExercise>
                {
                    new LoggedExercise { ExerciseName = SquatExerciseName, MetabolicEquivalent = SquatMetabolicEquivalent }
                },
                Duration = TimeSpan.FromMinutes(SixtyMinutesInSeconds)
            };

            this.workoutLogRepository.GetClientWeight(ValidClientIdentifier).Returns(DefaultClientWeightInKilograms);
            this.workoutLogRepository.SaveWorkoutLog(log).Returns(true);
            this.achievementsRepository.GetAchievementShowcaseForClient(ValidClientIdentifier).Returns(new List<AchievementShowcaseItem>());

            var result = this.systemUnderTest.FinalizeWorkout(log);

            result.Should().BeTrue();
            this.workoutLogRepository.Received(OneMethodCall).SaveWorkoutLog(log);
        }

        [Fact]
        public void SaveSet_NullLog_ReturnsFalse()
        {
            var result = this.systemUnderTest.SaveSet(null!, SquatExerciseName, new LoggedSet());

            result.Should().BeFalse();
        }

        [Fact]
        public void SaveSet_ValidSet_AddsSetToExerciseAndReturnsTrue()
        {
            var log = WorkoutLogFactory.CreateEmptyWorkoutLog(ValidClientIdentifier);
            var loggedSet = new LoggedSet();

            var result = this.systemUnderTest.SaveSet(log, SquatExerciseName, loggedSet);

            result.Should().BeTrue();
            log.Exercises.Should().ContainSingle(exercise => exercise.ExerciseName == SquatExerciseName);
            log.Exercises.First().Sets.Should().ContainSingle(set => set == loggedSet);
            loggedSet.SetIndex.Should().Be(FirstSetIndex);
        }

        [Fact]
        public void ModifyWorkout_NullLog_ReturnsFalse()
        {
            var result = this.systemUnderTest.ModifyWorkout(null!);

            result.Should().BeFalse();
        }

        [Fact]
        public void ModifyWorkout_ValidLog_ReturnsTrue()
        {
            var log = WorkoutLogFactory.CreateEmptyWorkoutLog();
            this.workoutLogRepository.SaveWorkoutLog(log).Returns(true);

            var result = this.systemUnderTest.ModifyWorkout(log);

            result.Should().BeTrue();
        }

        [Fact]
        public void ModifyWorkout_RepositoryThrowsException_ReturnsFalse()
        {
            var log = WorkoutLogFactory.CreateEmptyWorkoutLog();
            this.workoutLogRepository.When(x => x.SaveWorkoutLog(log)).Do(x => { throw new Exception(DatabaseErrorMessage); });

            var result = this.systemUnderTest.ModifyWorkout(log);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task SyncNutritionAsync_ValidPayload_ReturnsTrue()
        {
            var payload = new NutritionSyncPayload();
            var handlerMock = Substitute.For<HttpMessageHandler>();
            var client = new HttpClient(handlerMock) { BaseAddress = new Uri(NutritionSyncEndpointUrl) };

            this.httpClientFactory.CreateClient().Returns(client);

            var result = await this.systemUnderTest.SyncNutritionAsync(payload);
        }

        [Fact]
        public void BuildNutritionSyncPayload_ValidClientId_ReturnsExpectedPayload()
        {
            var clientId = ValidClientIdentifier;
            var moderateWorkoutLog = WorkoutLogFactory.CreateModerateIntensityWorkoutLog(clientId);
            moderateWorkoutLog.TotalCaloriesBurned = DefaultCaloriesBurned;
            moderateWorkoutLog.IntensityTag = HighIntensityTag;

            var history = new List<WorkoutLog>
            {
                moderateWorkoutLog
            };
            this.workoutLogRepository.GetWorkoutHistory(clientId).Returns(history);

            var clients = new List<Client>
            {
                new Client { Id = clientId, Weight = DefaultClientWeightInKilograms, Height = DefaultClientHeightInMeters }
            };
            this.trainerRepository.GetTrainerClients(Arg.Any<int>()).Returns(clients);

            var result = this.systemUnderTest.BuildNutritionSyncPayload(clientId);

            result.TotalCalories.Should().Be(DefaultCaloriesBurned);
            result.WorkoutDifficulty.Should().Be(HighIntensityTag);
            result.UserBmi.Should().BeGreaterThan(0);
        }

        [Fact]
        public void BuildClientProfileSnapshot_ValidClientIdWithWorkouts_ReturnsPopulatedSnapshot()
        {
            var clientId = ValidClientIdentifier;
            var moderateWorkoutLog = WorkoutLogFactory.CreateModerateIntensityWorkoutLog(clientId);
            moderateWorkoutLog.TotalCaloriesBurned = SnapshotCaloriesBurned;
            moderateWorkoutLog.WorkoutName = LegDayWorkoutName;

            var history = new List<WorkoutLog> { moderateWorkoutLog };
            this.workoutLogRepository.GetWorkoutHistory(clientId).Returns(history);

            var plan = new NutritionPlan { PlanId = DefaultPlanIdentifier, StartDate = DateTime.Today.AddDays(-OneDayDifference), EndDate = DateTime.Today.AddDays(OneDayDifference) };
            this.nutritionRepository.GetNutritionPlansForClient(clientId).Returns(new List<NutritionPlan> { plan });

            var meals = new List<Meal> { new Meal() };
            this.nutritionRepository.GetMealsForPlan(DefaultPlanIdentifier).Returns(meals);

            var result = this.systemUnderTest.BuildClientProfileSnapshot(clientId);

            result.CaloriesSummary.Should().Contain(SnapshotCaloriesBurned.ToString());
            result.LatestSessionHint.Should().Contain(LegDayWorkoutName);
            result.LoggedExercises.Should().HaveCount(moderateWorkoutLog.Exercises.Count);
            result.Meals.Should().HaveCount(ExpectedMealsCount);
        }

        [Fact]
        public void GetActiveNutritionPlan_InvalidClientId_ReturnsNull()
        {
            var result = this.systemUnderTest.GetActiveNutritionPlan(InvalidClientIdentifier);

            result.Should().BeNull();
        }

        [Fact]
        public void BuildEstimatedWorkoutDurationDisplay_NoExercises_ReturnsEmptyDurationPlaceholder()
        {
            var exercises = new List<LoggedExercise>();

            var result = this.systemUnderTest.BuildEstimatedWorkoutDurationDisplay(exercises);

            result.Should().Be(EmptyDurationDisplay);
        }

        [Fact]
        public void GetAvailableWorkoutsForClient_ValidClientId_ReturnsWorkouts()
        {
            var clientId = ValidClientIdentifier;
            var expectedWorkouts = new List<WorkoutTemplate> { new WorkoutTemplate { Id = ValidClientIdentifier } };
            this.workoutTemplateRepository.GetAvailableWorkouts(clientId).Returns(expectedWorkouts);

            var result = this.systemUnderTest.GetAvailableWorkoutsForClient(clientId);

            result.Should().BeEquivalentTo(expectedWorkouts);
        }
    }
}
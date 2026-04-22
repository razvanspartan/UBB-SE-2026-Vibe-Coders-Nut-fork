using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class ClientServiceTests
    {
        private const int ValidClientIdentifier = 1;
        private const int InvalidClientIdentifier = 0;
        private const double DefaultClientWeightInKilograms = 80;
        private const string NutritionSyncEndpointUrl = "http://localhost/sync";
        private const string SquatExerciseName = "Squat";
        private const float SquatMetabolicEquivalent = 5.0f;  // Changed from double to float
        private const int SixtyMinutesInSeconds = 60;
        private const string EmptyDurationDisplay = "00:00";
        private const int FirstSetIndex = 1;

        private readonly IRepositoryWorkoutLog _workoutLogRepository;
        private readonly ProgressionService _progressionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EvaluationEngine _evaluationEngine;
        private readonly IAchievementUnlockedBus _achievementBus;
        private readonly NutritionSyncOptions _nutritionSync;
        private readonly IRepositoryTrainer _trainerRepository;
        private readonly IRepositoryNotification _notificationRepository;
        private readonly IRepositoryAchievements _achievementsRepository;
        private readonly IRepositoryNutrition _nutritionRepository;
        private readonly IRepositoryWorkoutTemplate _workoutTemplateRepository;
        private readonly ClientService _systemUnderTest;

        public ClientServiceTests()
        {
            _workoutLogRepository = Substitute.For<IRepositoryWorkoutLog>();
            _progressionService = new ProgressionService(Substitute.For<IRepositoryWorkoutTemplate>(), Substitute.For<IRepositoryNotification>());
            _httpClientFactory = Substitute.For<IHttpClientFactory>();
            _evaluationEngine = new EvaluationEngine(Substitute.For<IRepositoryAchievements>());
            _achievementBus = Substitute.For<IAchievementUnlockedBus>();
            _nutritionSync = new NutritionSyncOptions { Endpoint = NutritionSyncEndpointUrl };
            _trainerRepository = Substitute.For<IRepositoryTrainer>();
            _notificationRepository = Substitute.For<IRepositoryNotification>();
            _achievementsRepository = Substitute.For<IRepositoryAchievements>();
            _nutritionRepository = Substitute.For<IRepositoryNutrition>();
            _workoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();

            _systemUnderTest = new ClientService(
                _workoutLogRepository,
                _progressionService,
                _httpClientFactory,
                _evaluationEngine,
                _achievementBus,
                _nutritionSync,
                _trainerRepository,
                _notificationRepository,
                _achievementsRepository,
                _nutritionRepository,
                _workoutTemplateRepository);
        }

        [Fact]
        public void GetWorkoutHistoryForClient_ValidClientId_ReturnsWorkoutLogs()
        {
            var clientId = ValidClientIdentifier;
            var expectedLogs = new List<WorkoutLog> { new WorkoutLog { Id = ValidClientIdentifier, ClientId = clientId } };
            _workoutLogRepository.GetWorkoutHistory(clientId).Returns(expectedLogs);

            var result = _systemUnderTest.GetWorkoutHistoryForClient(clientId);

            result.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public void GetWorkoutHistoryForClient_RepositoryThrowsException_ReturnsEmptyList()
        {
            var clientId = ValidClientIdentifier;
            _workoutLogRepository.When(x => x.GetWorkoutHistory(clientId)).Do(x => { throw new Exception("Database Error"); });

            var result = _systemUnderTest.GetWorkoutHistoryForClient(clientId);

            result.Should().BeEmpty();
        }

        [Fact]
        public void UpdateWorkoutLog_ValidLog_ReturnsTrue()
        {
            var log = new WorkoutLog { Id = ValidClientIdentifier };
            _workoutLogRepository.UpdateWorkoutLog(log).Returns(true);

            var result = _systemUnderTest.UpdateWorkoutLog(log);

            result.Should().BeTrue();
        }

        [Fact]
        public void UpdateWorkoutLog_RepositoryThrowsException_ReturnsFalse()
        {
            var log = new WorkoutLog { Id = ValidClientIdentifier };
            _workoutLogRepository.When(x => x.UpdateWorkoutLog(log)).Do(x => { throw new Exception("Database Error"); });

            var result = _systemUnderTest.UpdateWorkoutLog(log);

            result.Should().BeFalse();
        }

        [Fact]
        public void FinalizeWorkout_NullLog_ReturnsFalse()
        {
            var result = _systemUnderTest.FinalizeWorkout(null!);

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

            _workoutLogRepository.GetClientWeight(ValidClientIdentifier).Returns(DefaultClientWeightInKilograms);
            _workoutLogRepository.SaveWorkoutLog(log).Returns(true);
            _achievementsRepository.GetAchievementShowcaseForClient(ValidClientIdentifier).Returns(new List<AchievementShowcaseItem>());

            var result = _systemUnderTest.FinalizeWorkout(log);

            result.Should().BeTrue();
            _workoutLogRepository.Received(1).SaveWorkoutLog(log);
        }

        [Fact]
        public void SaveSet_NullLog_ReturnsFalse()
        {
            var result = _systemUnderTest.SaveSet(null!, SquatExerciseName, new LoggedSet());

            result.Should().BeFalse();
        }

        [Fact]
        public void SaveSet_ValidSet_AddsSetToExerciseAndReturnsTrue()
        {
            var log = new WorkoutLog { Id = ValidClientIdentifier, Exercises = new List<LoggedExercise>() };
            var loggedSet = new LoggedSet();

            var result = _systemUnderTest.SaveSet(log, SquatExerciseName, loggedSet);

            result.Should().BeTrue();
            log.Exercises.Should().ContainSingle(exercise => exercise.ExerciseName == SquatExerciseName);
            log.Exercises.First().Sets.Should().ContainSingle(set => set == loggedSet);
            loggedSet.SetIndex.Should().Be(FirstSetIndex);
        }

        [Fact]
        public void ModifyWorkout_NullLog_ReturnsFalse()
        {
            var result = _systemUnderTest.ModifyWorkout(null!);

            result.Should().BeFalse();
        }

        [Fact]
        public void ModifyWorkout_ValidLog_ReturnsTrue()
        {
            var log = new WorkoutLog { Id = ValidClientIdentifier };
            _workoutLogRepository.SaveWorkoutLog(log).Returns(true);

            var result = _systemUnderTest.ModifyWorkout(log);

            result.Should().BeTrue();
        }

        [Fact]
        public void GetActiveNutritionPlan_InvalidClientId_ReturnsNull()
        {
            var result = _systemUnderTest.GetActiveNutritionPlan(InvalidClientIdentifier);

            result.Should().BeNull();
        }

        [Fact]
        public void BuildEstimatedWorkoutDurationDisplay_NoExercises_ReturnsEmptyDurationPlaceholder()
        {
            var exercises = new List<LoggedExercise>();

            var result = _systemUnderTest.BuildEstimatedWorkoutDurationDisplay(exercises);

            result.Should().Be(EmptyDurationDisplay);
        }

        [Fact]
        public void GetAvailableWorkoutsForClient_ValidClientId_ReturnsWorkouts()
        {
            var clientId = ValidClientIdentifier;
            var expectedWorkouts = new List<WorkoutTemplate> { new WorkoutTemplate { Id = ValidClientIdentifier } };
            _workoutTemplateRepository.GetAvailableWorkouts(clientId).Returns(expectedWorkouts);

            var result = _systemUnderTest.GetAvailableWorkoutsForClient(clientId);

            result.Should().BeEquivalentTo(expectedWorkouts);
        }
    }
}
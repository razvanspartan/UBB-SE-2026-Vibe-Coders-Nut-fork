using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class ClientServiceTests
    {
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
        private readonly ClientService _sut;

        public ClientServiceTests()
        {
            _workoutLogRepository = Substitute.For<IRepositoryWorkoutLog>();
            _progressionService = new ProgressionService(Substitute.For<IRepositoryWorkoutTemplate>(), Substitute.For<IRepositoryNotification>());
            _httpClientFactory = Substitute.For<IHttpClientFactory>();
            _evaluationEngine = new EvaluationEngine(Substitute.For<IRepositoryAchievements>());
            _achievementBus = Substitute.For<IAchievementUnlockedBus>();
            _nutritionSync = new NutritionSyncOptions { Endpoint = "http://localhost/sync" };
            _trainerRepository = Substitute.For<IRepositoryTrainer>();
            _notificationRepository = Substitute.For<IRepositoryNotification>();
            _achievementsRepository = Substitute.For<IRepositoryAchievements>();
            _nutritionRepository = Substitute.For<IRepositoryNutrition>();
            _workoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();

            _sut = new ClientService(
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
            // Arrange
            var clientId = 1;
            var expectedLogs = new List<WorkoutLog> { new WorkoutLog { Id = 1, ClientId = clientId } };
            _workoutLogRepository.GetWorkoutHistory(clientId).Returns(expectedLogs);

            // Act
            var result = _sut.GetWorkoutHistoryForClient(clientId);

            // Assert
            result.Should().BeEquivalentTo(expectedLogs);
        }

        [Fact]
        public void GetWorkoutHistoryForClient_RepositoryThrowsException_ReturnsEmptyList()
        {
            // Arrange
            var clientId = 1;
            _workoutLogRepository.When(x => x.GetWorkoutHistory(clientId)).Do(x => { throw new Exception("DB Error"); });

            // Act
            var result = _sut.GetWorkoutHistoryForClient(clientId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void UpdateWorkoutLog_ValidLog_ReturnsTrue()
        {
            // Arrange
            var log = new WorkoutLog { Id = 1 };
            _workoutLogRepository.UpdateWorkoutLog(log).Returns(true);

            // Act
            var result = _sut.UpdateWorkoutLog(log);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void UpdateWorkoutLog_RepositoryThrowsException_ReturnsFalse()
        {
            // Arrange
            var log = new WorkoutLog { Id = 1 };
            _workoutLogRepository.When(x => x.UpdateWorkoutLog(log)).Do(x => { throw new Exception("DB Error"); });

            // Act
            var result = _sut.UpdateWorkoutLog(log);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void FinalizeWorkout_NullLog_ReturnsFalse()
        {
            // Act
            var result = _sut.FinalizeWorkout(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void FinalizeWorkout_ValidLog_ReturnsTrue()
        {
            // Arrange
            var log = new WorkoutLog 
            { 
                Id = 1, 
                ClientId = 1, 
                Exercises = new List<LoggedExercise>
                {
                    new LoggedExercise { ExerciseName = "Squat", MetabolicEquivalent = 5.0f }
                },
                Duration = TimeSpan.FromMinutes(60)
            };

            _workoutLogRepository.GetClientWeight(1).Returns(80);
            _workoutLogRepository.SaveWorkoutLog(log).Returns(true);
            _achievementsRepository.GetAchievementShowcaseForClient(1).Returns(new List<AchievementShowcaseItem>());

            // Act
            var result = _sut.FinalizeWorkout(log);

            // Assert
            result.Should().BeTrue();
            _workoutLogRepository.Received(1).SaveWorkoutLog(log);
        }

        [Fact]
        public void SaveSet_NullLog_ReturnsFalse()
        {
            // Act
            var result = _sut.SaveSet(null, "Squat", new LoggedSet());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void SaveSet_ValidSet_AddsSetToExerciseAndReturnsTrue()
        {
            // Arrange
            var log = new WorkoutLog { Id = 1, Exercises = new List<LoggedExercise>() };
            var set = new LoggedSet { ActualWeight = 100 };

            // Act
            var result = _sut.SaveSet(log, "Squat", set);

            // Assert
            result.Should().BeTrue();
            log.Exercises.Should().ContainSingle(e => e.ExerciseName == "Squat");
            log.Exercises.First().Sets.Should().ContainSingle(s => s == set);
            set.SetIndex.Should().Be(1);
        }

        [Fact]
        public void ModifyWorkout_NullLog_ReturnsFalse()
        {
            // Act
            var result = _sut.ModifyWorkout(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ModifyWorkout_ValidLog_ReturnsTrue()
        {
            // Arrange
            var log = new WorkoutLog { Id = 1 };
            _workoutLogRepository.SaveWorkoutLog(log).Returns(true);

            // Act
            var result = _sut.ModifyWorkout(log);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetActiveNutritionPlan_InvalidClientId_ReturnsNull()
        {
            // Act
            var result = _sut.GetActiveNutritionPlan(0);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void BuildEstimatedWorkoutDurationDisplay_NoExercises_Returns00_00()
        {
            // Arrange
            var exercises = new List<LoggedExercise>();

            // Act
            var result = _sut.BuildEstimatedWorkoutDurationDisplay(exercises);

            // Assert
            result.Should().Be("00:00");
        }

        [Fact]
        public void GetAvailableWorkoutsForClient_ValidClientId_ReturnsWorkouts()
        {
            // Arrange
            var clientId = 1;
            var expectedWorkouts = new List<WorkoutTemplate> { new WorkoutTemplate { Id = 1 } };
            _workoutTemplateRepository.GetAvailableWorkouts(clientId).Returns(expectedWorkouts);

            // Act
            var result = _sut.GetAvailableWorkoutsForClient(clientId);

            // Assert
            result.Should().BeEquivalentTo(expectedWorkouts);
        }
    }
}
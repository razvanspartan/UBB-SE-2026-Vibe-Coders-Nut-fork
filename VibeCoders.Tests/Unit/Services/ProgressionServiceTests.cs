using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class ProgressionServiceTests
    {
        private readonly IRepositoryWorkoutTemplate _workoutTemplateRepository;
        private readonly IRepositoryNotification _notificationRepository;
        private readonly ProgressionService _sut;

        public ProgressionServiceTests()
        {
            _workoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();
            _notificationRepository = Substitute.For<IRepositoryNotification>();
            _sut = new ProgressionService(_workoutTemplateRepository, _notificationRepository);
        }

        [Fact]
        public void EvaluateWorkout_NullLog_DoesNothing()
        {
            // Act
            var exception = Record.Exception(() => _sut.EvaluateWorkout(null!));

            // Assert
            exception.Should().BeNull();
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_NullExercises_DoesNothing()
        {
            // Arrange
            var log = new WorkoutLog { Exercises = null! };

            // Act
            var exception = Record.Exception(() => _sut.EvaluateWorkout(log));

            // Assert
            exception.Should().BeNull();
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_EmptySets_DoesNothing()
        {
            // Arrange
            var log = new WorkoutLog
            {
                ClientId = 1,
                Exercises = new List<LoggedExercise>
                {
                    new LoggedExercise { Sets = new List<LoggedSet>() }
                }
            };

            // Act
            _sut.EvaluateWorkout(log);

            // Assert
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_TemplateNotFound_DoesNotApplyProgressionOrNotification()
        {
            // Arrange
            var log = new WorkoutLog
            {
                ClientId = 1,
                Exercises = new List<LoggedExercise>
                {
                    new LoggedExercise
                    {
                        ParentTemplateExerciseId = 1,
                        Sets = new List<LoggedSet> { new LoggedSet { ActualReps = 10 } }
                    }
                }
            };

            _workoutTemplateRepository.GetTemplateExercise(1).Returns((TemplateExercise)null!);

            // Act
            _sut.EvaluateWorkout(log);

            // Assert
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().UpdateTemplateWeight(default, default);
            _notificationRepository.DidNotReceiveWithAnyArgs().SaveNotification(default!);
        }

        [Fact]
        public void EvaluateWorkout_ProgressionApplied_WhenRatioSufficient()
        {
            // Arrange
            var exercise = new LoggedExercise
            {
                ParentTemplateExerciseId = 1,
                Sets = new List<LoggedSet> { new LoggedSet { ActualReps = 10, ActualWeight = 50 } }
            };

            var log = new WorkoutLog
            {
                ClientId = 1,
                Exercises = new List<LoggedExercise> { exercise }
            };

            var template = new TemplateExercise
            {
                Id = 1,
                TargetReps = 10,
                TargetWeight = 50,
                MuscleGroup = MuscleGroup.CHEST
            };

            _workoutTemplateRepository.GetTemplateExercise(1).Returns(template);
            _workoutTemplateRepository.UpdateTemplateWeight(1, 52.5).Returns(true); // Assuming standard increment is 2.5

            // Act
            _sut.EvaluateWorkout(log);

            // Assert
            _workoutTemplateRepository.Received(1).UpdateTemplateWeight(1, 52.5);
            exercise.IsSystemAdjusted.Should().BeTrue();
            exercise.PerformanceRatio.Should().Be(1.0);
        }

        [Fact]
        public void EvaluateWorkout_PlateauDetected_RaisesNotification()
        {
            // Arrange
            var exercise = new LoggedExercise
            {
                ExerciseName = "Bench Press",
                ParentTemplateExerciseId = 1,
                Sets = new List<LoggedSet>
                {
                    new LoggedSet { ActualReps = 5, ActualWeight = 50 },
                    new LoggedSet { ActualReps = 5, ActualWeight = 50 }
                }
            };

            var log = new WorkoutLog
            {
                ClientId = 1,
                Exercises = new List<LoggedExercise> { exercise }
            };

            var template = new TemplateExercise
            {
                Id = 1,
                TargetReps = 10,
                TargetWeight = 50,
                MuscleGroup = MuscleGroup.CHEST
            };

            _workoutTemplateRepository.GetTemplateExercise(1).Returns(template);

            // Act
            _sut.EvaluateWorkout(log);

            // Assert
            _notificationRepository.Received(1).SaveNotification(Arg.Is<Notification>(n =>
                n.Type == NotificationType.Plateau &&
                n.ClientId == 1 &&
                n.RelatedId == 1));
            exercise.IsSystemAdjusted.Should().BeTrue();
        }

        [Fact]
        public void ProcessDeloadConfirmation_NullNotification_DoesNothing()
        {
            // Act
            var exception = Record.Exception(() => _sut.ProcessDeloadConfirmation(null!));

            // Assert
            exception.Should().BeNull();
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void ProcessDeloadConfirmation_TemplateNotFound_DoesNothing()
        {
            // Arrange
            var notification = new Notification(title: "test", message: "test", type: NotificationType.Plateau, relatedId: 1);
            _workoutTemplateRepository.GetTemplateExercise(1).Returns((TemplateExercise)null!);

            // Act
            _sut.ProcessDeloadConfirmation(notification);

            // Assert
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().UpdateTemplateWeight(default, default);
        }

        [Fact]
        public void ProcessDeloadConfirmation_ValidNotification_UpdatesTemplateWeight()
        {
            // Arrange
            var notification = new Notification(title: "test", message: "test", type: NotificationType.Plateau, relatedId: 1)
            {
                IsRead = false
            };
            var template = new TemplateExercise { Id = 1, TargetWeight = 100 };

            _workoutTemplateRepository.GetTemplateExercise(1).Returns(template);
            _workoutTemplateRepository.UpdateTemplateWeight(1, 90).Returns(true); // Assuming 10% deload

            // Act
            _sut.ProcessDeloadConfirmation(notification);

            // Assert
            _workoutTemplateRepository.Received(1).UpdateTemplateWeight(1, 90);
            notification.IsRead.Should().BeTrue();
        }
    }
}
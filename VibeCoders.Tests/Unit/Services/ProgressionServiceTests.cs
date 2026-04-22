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
        private const double WeightProgressionIncrement = 2.5;
        private const double DeloadPercentage = 0.9;
        private const int DefaultClientIdentifier = 1;
        private const int DefaultParentTemplateExerciseIdentifier = 1;
        private const int DefaultTargetRepetitions = 10;
        private const double DefaultTargetWeight = 50;
        private const double PerfectPerformanceRatio = 1.0;
        private const int PlateauRepetitionValue = 5;
        private const double PlateauWeightValue = 50;

        private readonly IRepositoryWorkoutTemplate _workoutTemplateRepository;
        private readonly IRepositoryNotification _notificationRepository;
        private readonly ProgressionService _systemUnderTest;

        public ProgressionServiceTests()
        {
            _workoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();
            _notificationRepository = Substitute.For<IRepositoryNotification>();
            _systemUnderTest = new ProgressionService(_workoutTemplateRepository, _notificationRepository);
        }

        [Fact]
        public void EvaluateWorkout_NullLog_DoesNothing()
        {
            var exception = Record.Exception(() => _systemUnderTest.EvaluateWorkout(null!));

            exception.Should().BeNull();
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_NullExercises_DoesNothing()
        {
            var log = new WorkoutLog { Exercises = null! };

            var exception = Record.Exception(() => _systemUnderTest.EvaluateWorkout(log));

            exception.Should().BeNull();
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_EmptySets_DoesNothing()
        {
            var log = new WorkoutLog
            {
                ClientId = DefaultClientIdentifier,
                Exercises = new List<LoggedExercise>
                {
                    new LoggedExercise { Sets = new List<LoggedSet>() }
                }
            };

            _systemUnderTest.EvaluateWorkout(log);

            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_TemplateNotFound_DoesNotApplyProgressionOrNotification()
        {
            var log = new WorkoutLog
            {
                ClientId = DefaultClientIdentifier,
                Exercises = new List<LoggedExercise>
                {
                    new LoggedExercise
                    {
                        ParentTemplateExerciseId = DefaultParentTemplateExerciseIdentifier,
                        Sets = new List<LoggedSet> { new LoggedSet { ActualReps = DefaultTargetRepetitions } }
                    }
                }
            };

            _workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns((TemplateExercise)null!);

            _systemUnderTest.EvaluateWorkout(log);

            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().UpdateTemplateWeight(default, default);
            _notificationRepository.DidNotReceiveWithAnyArgs().SaveNotification(default!);
        }

        [Fact]
        public void EvaluateWorkout_ProgressionApplied_WhenRatioSufficient()
        {
            var exercise = new LoggedExercise
            {
                ParentTemplateExerciseId = DefaultParentTemplateExerciseIdentifier,
                Sets = new List<LoggedSet> { new LoggedSet { ActualReps = DefaultTargetRepetitions, ActualWeight = DefaultTargetWeight } }
            };

            var log = new WorkoutLog
            {
                ClientId = DefaultClientIdentifier,
                Exercises = new List<LoggedExercise> { exercise }
            };

            var template = new TemplateExercise
            {
                Id = DefaultParentTemplateExerciseIdentifier,
                TargetReps = DefaultTargetRepetitions,
                TargetWeight = DefaultTargetWeight,
                MuscleGroup = MuscleGroup.CHEST
            };

            _workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns(template);
            _workoutTemplateRepository.UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, DefaultTargetWeight + WeightProgressionIncrement).Returns(true);

            _systemUnderTest.EvaluateWorkout(log);

            _workoutTemplateRepository.Received(1).UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, DefaultTargetWeight + WeightProgressionIncrement);
            exercise.IsSystemAdjusted.Should().BeTrue();
            exercise.PerformanceRatio.Should().Be(PerfectPerformanceRatio);
        }

        [Fact]
        public void EvaluateWorkout_PlateauDetected_RaisesNotification()
        {
            var exercise = new LoggedExercise
            {
                ExerciseName = "Bench Press",
                ParentTemplateExerciseId = DefaultParentTemplateExerciseIdentifier,
                Sets = new List<LoggedSet>
                {
                    new LoggedSet { ActualReps = PlateauRepetitionValue, ActualWeight = PlateauWeightValue },
                    new LoggedSet { ActualReps = PlateauRepetitionValue, ActualWeight = PlateauWeightValue }
                }
            };

            var log = new WorkoutLog
            {
                ClientId = DefaultClientIdentifier,
                Exercises = new List<LoggedExercise> { exercise }
            };

            var template = new TemplateExercise
            {
                Id = DefaultParentTemplateExerciseIdentifier,
                TargetReps = DefaultTargetRepetitions,
                TargetWeight = DefaultTargetWeight,
                MuscleGroup = MuscleGroup.CHEST
            };

            _workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns(template);

            _systemUnderTest.EvaluateWorkout(log);

            _notificationRepository.Received(1).SaveNotification(Arg.Is<Notification>(notification =>
                notification.Type == NotificationType.Plateau &&
                notification.ClientId == DefaultClientIdentifier &&
                notification.RelatedId == DefaultParentTemplateExerciseIdentifier));
            exercise.IsSystemAdjusted.Should().BeTrue();
        }

        [Fact]
        public void ProcessDeloadConfirmation_NullNotification_DoesNothing()
        {
            var exception = Record.Exception(() => _systemUnderTest.ProcessDeloadConfirmation(null!));

            exception.Should().BeNull();
            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void ProcessDeloadConfirmation_TemplateNotFound_DoesNothing()
        {
            var notification = new Notification(title: "test", message: "test", type: NotificationType.Plateau, relatedId: DefaultParentTemplateExerciseIdentifier);
            _workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns((TemplateExercise)null!);

            _systemUnderTest.ProcessDeloadConfirmation(notification);

            _workoutTemplateRepository.DidNotReceiveWithAnyArgs().UpdateTemplateWeight(default, default);
        }

        [Fact]
        public void ProcessDeloadConfirmation_ValidNotification_UpdatesTemplateWeight()
        {
            var notification = new Notification(title: "test", message: "test", type: NotificationType.Plateau, relatedId: DefaultParentTemplateExerciseIdentifier)
            {
                IsRead = false
            };
            var template = new TemplateExercise { Id = DefaultParentTemplateExerciseIdentifier, TargetWeight = 100 };

            _workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns(template);
            _workoutTemplateRepository.UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, 100 * DeloadPercentage).Returns(true);

            _systemUnderTest.ProcessDeloadConfirmation(notification);

            _workoutTemplateRepository.Received(1).UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, 100 * DeloadPercentage);
            notification.IsRead.Should().BeTrue();
        }
    }
}
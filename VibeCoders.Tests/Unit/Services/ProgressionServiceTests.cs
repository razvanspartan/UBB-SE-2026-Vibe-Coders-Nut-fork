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

        private readonly IRepositoryWorkoutTemplate workoutTemplateRepository;
        private readonly IRepositoryNotification notificationRepository;
        private readonly ProgressionService systemUnderTest;

        public ProgressionServiceTests()
        {
            workoutTemplateRepository = Substitute.For<IRepositoryWorkoutTemplate>();
            notificationRepository = Substitute.For<IRepositoryNotification>();
            systemUnderTest = new ProgressionService(workoutTemplateRepository, notificationRepository);
        }

        [Fact]
        public void EvaluateWorkout_NullLog_DoesNothing()
        {
            var exception = Record.Exception(() => systemUnderTest.EvaluateWorkout(null!));

            exception.Should().BeNull();
            workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void EvaluateWorkout_NullExercises_DoesNothing()
        {
            var log = new WorkoutLog { Exercises = null! };

            var exception = Record.Exception(() => systemUnderTest.EvaluateWorkout(log));

            exception.Should().BeNull();
            workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
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

            systemUnderTest.EvaluateWorkout(log);

            workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
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

            workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns((TemplateExercise)null!);

            systemUnderTest.EvaluateWorkout(log);

            workoutTemplateRepository.DidNotReceiveWithAnyArgs().UpdateTemplateWeight(default, default);
            notificationRepository.DidNotReceiveWithAnyArgs().SaveNotification(default!);
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

            workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns(template);
            workoutTemplateRepository.UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, DefaultTargetWeight + WeightProgressionIncrement).Returns(true);

            systemUnderTest.EvaluateWorkout(log);

            workoutTemplateRepository.Received(1).UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, DefaultTargetWeight + WeightProgressionIncrement);
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

            workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns(template);

            systemUnderTest.EvaluateWorkout(log);

            notificationRepository.Received(1).SaveNotification(Arg.Is<Notification>(notification =>
                notification.Type == NotificationType.Plateau &&
                notification.ClientId == DefaultClientIdentifier &&
                notification.RelatedId == DefaultParentTemplateExerciseIdentifier));
            exercise.IsSystemAdjusted.Should().BeTrue();
        }

        [Fact]
        public void ProcessDeloadConfirmation_NullNotification_DoesNothing()
        {
            var exception = Record.Exception(() => systemUnderTest.ProcessDeloadConfirmation(null!));

            exception.Should().BeNull();
            workoutTemplateRepository.DidNotReceiveWithAnyArgs().GetTemplateExercise(default);
        }

        [Fact]
        public void ProcessDeloadConfirmation_TemplateNotFound_DoesNothing()
        {
            var notification = new Notification(title: "test", message: "test", type: NotificationType.Plateau, relatedId: DefaultParentTemplateExerciseIdentifier);
            workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns((TemplateExercise)null!);

            systemUnderTest.ProcessDeloadConfirmation(notification);

            workoutTemplateRepository.DidNotReceiveWithAnyArgs().UpdateTemplateWeight(default, default);
        }

        [Fact]
        public void ProcessDeloadConfirmation_ValidNotification_UpdatesTemplateWeight()
        {
            var notification = new Notification(title: "test", message: "test", type: NotificationType.Plateau, relatedId: DefaultParentTemplateExerciseIdentifier)
            {
                IsRead = false
            };
            var template = new TemplateExercise { Id = DefaultParentTemplateExerciseIdentifier, TargetWeight = 100 };

            workoutTemplateRepository.GetTemplateExercise(DefaultParentTemplateExerciseIdentifier).Returns(template);
            workoutTemplateRepository.UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, 100 * DeloadPercentage).Returns(true);

            systemUnderTest.ProcessDeloadConfirmation(notification);

            workoutTemplateRepository.Received(1).UpdateTemplateWeight(DefaultParentTemplateExerciseIdentifier, 100 * DeloadPercentage);
            notification.IsRead.Should().BeTrue();
        }
    }
}
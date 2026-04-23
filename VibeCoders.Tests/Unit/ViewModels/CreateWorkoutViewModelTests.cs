using System.Collections.Generic;
using System.ComponentModel;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class CreateWorkoutViewModelTests
    {
        private readonly ITrainerService trainerServiceMock;
        private readonly CreateWorkoutViewModel systemUnderTest;

        public CreateWorkoutViewModelTests()
        {
            this.trainerServiceMock = Substitute.For<ITrainerService>();
            this.trainerServiceMock.GetAllExerciseNames().Returns(new List<string> { "Squat", "Bench" }); 
            this.systemUnderTest = new CreateWorkoutViewModel(this.trainerServiceMock);
        }

        [Fact]
        public void Constructor_InitializesPropertiesAndLoadsExercises()
        {
            this.systemUnderTest.AvailableExercises.Should().Contain("Squat", "Bench");
            this.systemUnderTest.MuscleGroups.Should().NotBeEmpty();
            this.systemUnderTest.NewExerciseSets.Should().Be(3);
            this.systemUnderTest.NewExerciseReps.Should().Be(10);
            this.systemUnderTest.Exercises.Should().BeEmpty();
        }

        [Theory]
        [InlineData("WorkoutName", "Leg Day")]
        [InlineData("SelectedNewExercise", "Squat")]
        [InlineData("NewExerciseSets", 5.0)]
        [InlineData("NewExerciseReps", 12.0)]
        public void Properties_WhenSet_RaisePropertyChanged(string propertyName, object newValue)
        {

            var raisedEvents = new List<string>();
            this.systemUnderTest.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != null) raisedEvents.Add(args.PropertyName);
            };


            switch (propertyName)
            {
                case "WorkoutName":
                    this.systemUnderTest.WorkoutName = (string)newValue;
                    break;
                case "SelectedNewExercise":
                    this.systemUnderTest.SelectedNewExercise = (string)newValue;
                    break;
                case "NewExerciseSets":
                    this.systemUnderTest.NewExerciseSets = (double)newValue;
                    break;
                case "NewExerciseReps":
                    this.systemUnderTest.NewExerciseReps = (double)newValue;
                    break;
            }


            raisedEvents.Should().Contain(propertyName);
        }

        [Fact]
        public void AddExerciseCommand_WhenExerciseNotSelected_DoesNothing()
        {
            this.systemUnderTest.SelectedNewExercise = null;
            this.systemUnderTest.AddExerciseCommand.Execute(null);

            this.systemUnderTest.Exercises.Should().BeEmpty();
        }

        [Fact]
        public void AddExerciseCommand_WhenExerciseSelected_AddsToCollectionAndClearsSelection()
        {
            this.systemUnderTest.SelectedNewExercise = "Deadlift";
            this.systemUnderTest.NewExerciseReps = 5;
            this.systemUnderTest.NewExerciseSets = 5;

            this.systemUnderTest.AddExerciseCommand.Execute(null);

            this.systemUnderTest.Exercises.Should().ContainSingle();
            var exercise = this.systemUnderTest.Exercises[0];
            exercise.Name.Should().Be("Deadlift");
            exercise.TargetReps.Should().Be(5);
            exercise.TargetSets.Should().Be(5);
            exercise.MuscleGroup.Should().Be(MuscleGroup.OTHER);
            this.systemUnderTest.SelectedNewExercise.Should().BeNull();
        }

        [Fact]
        public void RemoveExerciseCommand_WhenExerciseIsNull_DoesNothing()
        {
            var exercise = new TemplateExercise { Name = "Pushup" };
            this.systemUnderTest.Exercises.Add(exercise);

            this.systemUnderTest.RemoveExerciseCommand.Execute(null);

            this.systemUnderTest.Exercises.Should().ContainSingle();
        }

        [Fact]
        public void RemoveExerciseCommand_WhenExerciseIsValid_RemovesFromCollection()
        {
            var exercise = new TemplateExercise { Name = "Pullup" };
            this.systemUnderTest.Exercises.Add(exercise);

            this.systemUnderTest.RemoveExerciseCommand.Execute(exercise);

            this.systemUnderTest.Exercises.Should().BeEmpty();
        }

        [Theory]
        [InlineData("", 1)]
        [InlineData(null, 1)]
        [InlineData("Valid", 0)]
        public void SaveWorkoutCommand_WhenInvalidState_DoesNotSave(string workoutName, int exerciseCount)
        {
            this.systemUnderTest.WorkoutName = workoutName;
            if (exerciseCount > 0)
            {
                this.systemUnderTest.Exercises.Add(new TemplateExercise());
            }

            var savedInvoked = false;
            this.systemUnderTest.WorkoutSaved += () => savedInvoked = true;

            this.systemUnderTest.SaveWorkoutCommand.Execute(null);

            this.trainerServiceMock.DidNotReceiveWithAnyArgs().SaveTrainerWorkout(default!);
            savedInvoked.Should().BeFalse();
        }

        [Fact]
        public void SaveWorkoutCommand_WhenValid_SavesAndInvokesEvent()
        {
            this.systemUnderTest.WorkoutName = "Back Day";
            this.systemUnderTest.ClientId = 1;
            this.systemUnderTest.Exercises.Add(new TemplateExercise { Name = "Row" });

            var savedInvoked = false;
            this.systemUnderTest.WorkoutSaved += () => savedInvoked = true;

            this.systemUnderTest.SaveWorkoutCommand.Execute(null);

            this.trainerServiceMock.Received(1).SaveTrainerWorkout(Arg.Is<WorkoutTemplate>(workout =>
                workout.Name == "Back Day" &&
                workout.ClientId == 1 &&
                workout.Type == WorkoutType.CUSTOM &&
                workout.GetExercises().Count == 1));
            savedInvoked.Should().BeTrue();
        }
    }
}


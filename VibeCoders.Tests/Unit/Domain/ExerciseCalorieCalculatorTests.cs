using System;
using FluentAssertions;
using VibeCoders.Domain;
using Xunit;

namespace VibeCoders.Tests.Unit.Domain
{
    public class ExerciseCalorieCalculatorTests
    {
        private const string KnownExercise = "Pull-Ups";
        private const string UnknownExercise = "Mystery Exercise";
        private const double KnownMetabolicEquivalent = 8.0;
        private const double DefaultMetabolicEquivalent = 5.0;
        private const double Weight = 80.0;
        private const int ExpectedCalories = 200;

        private static readonly TimeSpan ThirtyMinuteDuration = TimeSpan.FromMinutes(30);

        [Fact]
        public void GetMetabolicEquivalent_WhenExerciseExists_ReturnsMappedValue()
        {
            var metabolicEquivalent = ExerciseCalorieCalculator.GetMetabolicEquivalent(KnownExercise);

            metabolicEquivalent.Should().Be(KnownMetabolicEquivalent);
        }

        [Fact]
        public void GetMetabolicEquivalent_WhenExerciseDoesNotExist_ReturnsDefaultValue()
        {
            var metabolicEquivalent = ExerciseCalorieCalculator.GetMetabolicEquivalent(UnknownExercise);

            metabolicEquivalent.Should().Be(DefaultMetabolicEquivalent);
        }

        [Fact]
        public void CalculateCalories_WhenValuesAreValid_ReturnsRoundedCalories()
        {
            var calories = ExerciseCalorieCalculator.CalculateCalories(DefaultMetabolicEquivalent, Weight, ThirtyMinuteDuration);

            calories.Should().Be(ExpectedCalories);
        }
    }
}
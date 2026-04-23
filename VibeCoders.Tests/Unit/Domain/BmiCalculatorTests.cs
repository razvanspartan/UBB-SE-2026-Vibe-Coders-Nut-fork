using System;
using FluentAssertions;
using VibeCoders.Domain;
using Xunit;

namespace VibeCoders.Tests.Unit.Domain
{
    public class BmiCalculatorTests
    {
        private const double Weight = 80.0;
        private const double Height = 180.0;
        private const double ExpectedBmi = 24.69;
        private const double InvalidWeight = 0.0;
        private const double InvalidHeight = 0.0;

        [Fact]
        public void CalculateBmi_WhenWeightAndHeightAreValid_ReturnsRoundedBodyMassIndex()
        {
            var bmi = BmiCalculator.CalculateBmi(Weight, Height);

            bmi.Should().Be(ExpectedBmi);
        }

        [Fact]
        public void CalculateBmi_WhenWeightIsNotPositive_ThrowsArgumentOutOfRangeException()
        {
            Action calculateBmi = () => BmiCalculator.CalculateBmi(InvalidWeight, Height);

            calculateBmi.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CalculateBmi_WhenHeightIsNotPositive_ThrowsArgumentOutOfRangeException()
        {
            Action calculateBmi = () => BmiCalculator.CalculateBmi(Weight, InvalidHeight);

            calculateBmi.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
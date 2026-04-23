using System.Collections.Generic;
using FluentAssertions;
using VibeCoders.Models;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class ExerciseDisplayRowTests
    {
        private readonly ExerciseDisplayRow systemUnderTest;

        public ExerciseDisplayRowTests()
        {
            this.systemUnderTest = new ExerciseDisplayRow();
        }

        [Fact]
        public void GetReps_WhenSetsIsNull_ReturnsHyphen()
        {
            this.systemUnderTest.Sets = null!;

            var result = this.systemUnderTest.GetReps(0);

            result.Should().Be("-");
        }

        [Fact]
        public void GetReps_WhenIndexIsOutOfBounds_ReturnsHyphen()
        {
            this.systemUnderTest.Sets = new List<LoggedSet>();

            var result = this.systemUnderTest.GetReps(0);

            result.Should().Be("-");
        }

        [Fact]
        public void GetReps_WhenActualRepsIsNull_ReturnsHyphen()
        {
            this.systemUnderTest.Sets.Add(new LoggedSet { ActualReps = null });

            var result = this.systemUnderTest.GetReps(0);

            result.Should().Be("-");
        }

        [Fact]
        public void GetReps_WhenActualRepsHasValue_ReturnsStringValue()
        {
            this.systemUnderTest.Sets.Add(new LoggedSet { ActualReps = 12 });

            var result = this.systemUnderTest.GetReps(0);

            result.Should().Be("12");
        }

        [Fact]
        public void GetWeight_WhenSetsIsNull_ReturnsHyphen()
        {
            this.systemUnderTest.Sets = null!;

            var result = this.systemUnderTest.GetWeight(0);

            result.Should().Be("-");
        }

        [Fact]
        public void GetWeight_WhenIndexIsOutOfBounds_ReturnsHyphen()
        {
            this.systemUnderTest.Sets = new List<LoggedSet>();

            var result = this.systemUnderTest.GetWeight(0);

            result.Should().Be("-");
        }

        [Fact]
        public void GetWeight_WhenActualWeightIsNull_ReturnsHyphen()
        {
            this.systemUnderTest.Sets.Add(new LoggedSet { ActualWeight = null });

            var result = this.systemUnderTest.GetWeight(0);

            result.Should().Be("-");
        }

        [Fact]
        public void GetWeight_WhenActualWeightHasValue_ReturnsStringValue()
        {
            this.systemUnderTest.Sets.Add(new LoggedSet { ActualWeight = 55.5 });

            var result = this.systemUnderTest.GetWeight(0);

            result.Should().Be("55.5");
        }
    }
}

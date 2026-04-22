using System;
using FluentAssertions;
using VibeCoders.Converters;
using Xunit;

namespace VibeCoders.Tests.Unit.Converters
{
    public class BoolToOpacityConverterTests
    {
        private static readonly Type OpacityTargetType = typeof(double);
        private const string DefaultLanguageCode = "en-US";
        private const double FullyVisibleOpacity = 1.0;
        private const double ReducedOpacity = 0.5;

        private readonly BoolToOpacityConverter booleanToOpacityConverter = new();

        [Fact]
        public void Convert_WhenValueIsTrue_ReturnsFullyVisibleOpacity()
        {
            var convertedValue = this.booleanToOpacityConverter.Convert(true, OpacityTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(FullyVisibleOpacity);
        }

        [Fact]
        public void Convert_WhenValueIsFalse_ReturnsReducedOpacity()
        {
            var convertedValue = this.booleanToOpacityConverter.Convert(false, OpacityTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(ReducedOpacity);
        }

        [Fact]
        public void Convert_WhenValueIsNotBoolean_ReturnsFullyVisibleOpacity()
        {
            var convertedValue = this.booleanToOpacityConverter.Convert("not a boolean", OpacityTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(FullyVisibleOpacity);
        }

        [Fact]
        public void ConvertBack_Always_ThrowsNotImplementedException()
        {
            Action convertBack = () => this.booleanToOpacityConverter.ConvertBack(FullyVisibleOpacity, OpacityTargetType, null!, DefaultLanguageCode);

            convertBack.Should().Throw<NotImplementedException>();
        }
    }
}
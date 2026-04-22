using System;
using FluentAssertions;
using VibeCoders.Converters;
using Xunit;

namespace VibeCoders.Tests.Unit.Converters
{
    public class BoolNegateConverterTests
    {
        private static readonly Type BooleanTargetType = typeof(bool);
        private const string DefaultLanguageCode = "en-US";

        private readonly BoolNegateConverter booleanNegateConverter = new();

        [Fact]
        public void Convert_WhenValueIsTrue_ReturnsFalse()
        {
            var convertedValue = this.booleanNegateConverter.Convert(true, BooleanTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(false);
        }

        [Fact]
        public void Convert_WhenValueIsFalse_ReturnsTrue()
        {
            var convertedValue = this.booleanNegateConverter.Convert(false, BooleanTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(true);
        }

        [Fact]
        public void Convert_WhenValueIsNotBoolean_ReturnsOriginalValue()
        {
            const string originalTextValue = "keep me";

            var convertedValue = this.booleanNegateConverter.Convert(originalTextValue, BooleanTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().BeSameAs(originalTextValue);
        }

        [Fact]
        public void ConvertBack_WhenValueIsTrue_ReturnsFalse()
        {
            var convertedBackValue = this.booleanNegateConverter.ConvertBack(true, BooleanTargetType, null!, DefaultLanguageCode);

            convertedBackValue.Should().Be(false);
        }

        [Fact]
        public void ConvertBack_WhenValueIsNotBoolean_ReturnsOriginalValue()
        {
            const string originalTextValue = "keep me";

            var convertedBackValue = this.booleanNegateConverter.ConvertBack(originalTextValue, BooleanTargetType, null!, DefaultLanguageCode);

            convertedBackValue.Should().BeSameAs(originalTextValue);
        }
    }
}
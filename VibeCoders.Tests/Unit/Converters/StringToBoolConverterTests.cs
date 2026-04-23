using System;
using FluentAssertions;
using VibeCoders.Converters;
using Xunit;

namespace VibeCoders.Tests.Unit.Converters
{
    public class StringToBoolConverterTests
    {
        private static readonly Type BooleanTargetType = typeof(bool);
        private const string DefaultLanguageCode = "en-US";
        private const string NonEmptyStringValue = "content";
        private const string EmptyStringValue = "";

        private readonly StringToBoolConverter stringToBooleanConverter = new();

        [Fact]
        public void Convert_WhenValueIsNonEmptyString_ReturnsTrue()
        {
            var convertedValue = this.stringToBooleanConverter.Convert(NonEmptyStringValue, BooleanTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(true);
        }

        [Fact]
        public void Convert_WhenValueIsEmptyString_ReturnsFalse()
        {
            var convertedValue = this.stringToBooleanConverter.Convert(EmptyStringValue, BooleanTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(false);
        }

        [Fact]
        public void Convert_WhenValueIsNotString_ReturnsFalse()
        {
            var convertedValue = this.stringToBooleanConverter.Convert(123, BooleanTargetType, null!, DefaultLanguageCode);

            convertedValue.Should().Be(false);
        }

        [Fact]
        public void ConvertBack_Always_ThrowsNotSupportedException()
        {
            Action convertBack = () => this.stringToBooleanConverter.ConvertBack(true, BooleanTargetType, null!, DefaultLanguageCode);

            convertBack.Should().Throw<NotSupportedException>();
        }
    }
}
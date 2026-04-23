using System;
using FluentAssertions;
using Microsoft.UI.Xaml;
using VibeCoders.Converters;
using Xunit;

namespace VibeCoders.Tests.Unit.Converters
{
    public class BoolToVisibilityConverterTests
    {
        private const string DefaultLanguageCode = "en-US";
        private const string InvertVisibilityParameterValue = "Invert";

        private readonly BoolToVisibilityConverter booleanToVisibilityConverter = new();

        [Fact]
        public void Convert_WhenValueIsTrue_ReturnsVisible()
        {
            var convertedValue = this.booleanToVisibilityConverter.Convert(true, typeof(Visibility), null!, DefaultLanguageCode);

            convertedValue.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void Convert_WhenValueIsFalse_ReturnsCollapsed()
        {
            var convertedValue = this.booleanToVisibilityConverter.Convert(false, typeof(Visibility), null!, DefaultLanguageCode);

            convertedValue.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void Convert_WhenInvertVisibilityParameterIsPassed_ReturnsCollapsedForTrueValue()
        {
            var convertedValue = this.booleanToVisibilityConverter.Convert(true, typeof(Visibility), InvertVisibilityParameterValue, DefaultLanguageCode);

            convertedValue.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void Convert_WhenValueIsNotBoolean_ReturnsCollapsed()
        {
            var convertedValue = this.booleanToVisibilityConverter.Convert("not a boolean", typeof(Visibility), null!, DefaultLanguageCode);

            convertedValue.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void ConvertBack_Always_ThrowsNotSupportedException()
        {
            Action convertBack = () => this.booleanToVisibilityConverter.ConvertBack(Visibility.Visible, typeof(Visibility), null!, DefaultLanguageCode);

            convertBack.Should().Throw<NotSupportedException>();
        }
    }
}
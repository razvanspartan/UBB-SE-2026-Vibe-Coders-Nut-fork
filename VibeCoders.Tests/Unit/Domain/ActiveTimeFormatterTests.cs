using System;
using FluentAssertions;
using VibeCoders.Domain;
using Xunit;

namespace VibeCoders.Tests.Unit.Domain
{
    public class ActiveTimeFormatterTests
    {
        private const string ExpectedFormattedTime = "1:02:03";
        private const string ExpectedZeroTime = "0:00:00";
        private const int NinetyMinutesInSeconds = 5400;
        private const double ExpectedDecimalHours = 1.5;
        private const double ZeroHours = 0.0;

        private static readonly TimeSpan OneHourTwoMinutesThreeSeconds = new(0, 1, 2, 3);
        private static readonly TimeSpan NegativeDuration = TimeSpan.FromSeconds(-1);

        [Fact]
        public void ToHourMinuteSecond_WhenDurationIsPositive_ReturnsFormattedString()
        {
            var formattedTime = ActiveTimeFormatter.ToHourMinuteSecond(OneHourTwoMinutesThreeSeconds);

            formattedTime.Should().Be(ExpectedFormattedTime);
        }

        [Fact]
        public void ToHourMinuteSecond_WhenDurationIsNegative_ReturnsZeroFormattedString()
        {
            var formattedTime = ActiveTimeFormatter.ToHourMinuteSecond(NegativeDuration);

            formattedTime.Should().Be(ExpectedZeroTime);
        }

        [Fact]
        public void ToDecimalHours_WhenDurationIsNegative_ReturnsZero()
        {
            var hours = ActiveTimeFormatter.ToDecimalHours(NegativeDuration);

            hours.Should().Be(ZeroHours);
        }

        [Fact]
        public void ToDecimalHours_WhenDurationSecondsArePositive_ReturnsDecimalHours()
        {
            var hours = ActiveTimeFormatter.ToDecimalHours(NinetyMinutesInSeconds);

            hours.Should().Be(ExpectedDecimalHours);
        }
    }
}
using System;
using FluentAssertions;
using VibeCoders.Domain;
using Xunit;

namespace VibeCoders.Tests.Unit.Domain
{
    public class LevelingTierEvaluatorTests
    {
        private const int AchievementCount = 5;
        private const int UnrankedLevel = 0;
        private const string UnrankedTitle = "Unranked";
        private const int ExpectedLevel = 5;
        private const string ExpectedTitle = "Gym Enthusiast";
        private const string ExpectedTierDescription = "Level 5 Gym Enthusiast @ 5 achievements";

        [Fact]
        public void Evaluate_WhenAchievementCountMatchesDefaultTier_ReturnsExpectedLevelingResult()
        {
            var expectedLevelingResult = new LevelingResult(ExpectedLevel, ExpectedTitle);

            var levelingResult = LevelingTierEvaluator.Evaluate(AchievementCount);

            object.Equals(levelingResult, expectedLevelingResult).Should().BeTrue();
        }

        [Fact]
        public void Evaluate_WhenTierListIsEmpty_ReturnsUnrankedResult()
        {
            var expectedLevelingResult = new LevelingResult(UnrankedLevel, UnrankedTitle);

            var levelingResult = LevelingTierEvaluator.Evaluate(AchievementCount, Array.Empty<LevelTier>());

            object.Equals(levelingResult, expectedLevelingResult).Should().BeTrue();
        }

        [Fact]
        public void ToString_WhenCalledOnLevelTier_ReturnsExpectedDescription()
        {
            var levelTier = new LevelTier(ExpectedLevel, ExpectedTitle, AchievementCount);

            var description = levelTier.ToString();

            description.Should().Be(ExpectedTierDescription);
        }
    }
}
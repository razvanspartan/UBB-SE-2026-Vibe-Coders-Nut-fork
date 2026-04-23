using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class EvaluationEngineTests
    {
        private readonly IRepositoryAchievements mockAchievementRepository;
        private readonly EvaluationEngine evaluationEngine;
        private readonly int clientId = 1;

        public EvaluationEngineTests()
        {
            this.mockAchievementRepository = Substitute.For<IRepositoryAchievements>();
            this.evaluationEngine = new EvaluationEngine(this.mockAchievementRepository);
        }

        [Fact]
        public void Evaluate_Should_ReturnEmptyList_When_AllAchievementsAreAlreadyUnlocked()
        {
            var showcase = new List<AchievementShowcaseItem>
            {
                new AchievementShowcaseItem { AchievementId = 1, Title = "3-Day Streak", IsUnlocked = true },
                new AchievementShowcaseItem { AchievementId = 2, Title = "Iron Week", IsUnlocked = true }
            };

            this.mockAchievementRepository.GetAchievementShowcaseForClient(this.clientId)
                .Returns(showcase);

            var result = this.evaluationEngine.Evaluate(this.clientId);

            result.Should().BeEmpty();

            this.mockAchievementRepository.DidNotReceiveWithAnyArgs().AwardAchievement(default, default);
        }

        [Fact]
        public void Evaluate_Should_HandleEmptyShowcase_Gracefully()
        {
            this.mockAchievementRepository.GetAchievementShowcaseForClient(this.clientId)
                .Returns(new List<AchievementShowcaseItem>());

            var result = this.evaluationEngine.Evaluate(this.clientId);

            result.Should().BeEmpty();
        }

        [Fact]
        public void BuildRankShowcase_Should_ReturnLevel1_When_NoAchievementsUnlocked()
        {
            var showcase = new List<AchievementShowcaseItem>
            {
                new AchievementShowcaseItem { Title = "Test", IsUnlocked = false }
            };

            this.mockAchievementRepository.GetAchievementShowcaseForClient(this.clientId)
                .Returns(showcase);

            var result = this.evaluationEngine.BuildRankShowcase(this.clientId);

            result.DisplayLevel.Should().Be(1);
            result.UnlockedAchievementsDisplay.Should().Be("0 achievements unlocked");
            result.HasNextRank.Should().BeTrue();
        }

        [Fact]
        public void BuildRankShowcase_Should_UseSingularForm_When_OnlyOneAchievementUnlocked()
        {
            var showcase = new List<AchievementShowcaseItem>
            {
                new AchievementShowcaseItem { Title = "First Step", IsUnlocked = true },
                new AchievementShowcaseItem { Title = "Locked One", IsUnlocked = false }
            };

            this.mockAchievementRepository.GetAchievementShowcaseForClient(this.clientId)
                .Returns(showcase);

            var result = this.evaluationEngine.BuildRankShowcase(this.clientId);

            result.UnlockedAchievementsDisplay.Should().Be("1 achievement unlocked");
        }

        [Fact]
        public void BuildRankShowcase_Should_CalculateProgressCorrectly()
        {
            var showcase = new List<AchievementShowcaseItem>();
            for (int i = 0; i < 9; i++)
            {
                showcase.Add(new AchievementShowcaseItem { IsUnlocked = true });
            }

            this.mockAchievementRepository.GetAchievementShowcaseForClient(this.clientId)
                .Returns(showcase);

            var result = this.evaluationEngine.BuildRankShowcase(this.clientId);

            result.Should().NotBeNull();
            result.ProgressPercent.Should().BeGreaterThan(0);
            result.NextRankInfo.Should().Contain("more achievement");
        }
    }
}
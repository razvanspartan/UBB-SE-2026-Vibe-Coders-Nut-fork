using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class AchievementsViewModelTests
    {
        private readonly IRepositoryAchievements _achievementsRepository;
        private readonly AchievementsViewModel _sut;

        public AchievementsViewModelTests()
        {
            _achievementsRepository = Substitute.For<IRepositoryAchievements>();
            _sut = new AchievementsViewModel(_achievementsRepository);
        }

        [Fact]
        public void LoadAchievementsCommand_ValidClientId_PopulatesAchievementsAndUpdatesLoadingState()
        {
            // Arrange
            var clientId = 1;
            var mockShowcaseItems = new List<AchievementShowcaseItem>
            {
                new AchievementShowcaseItem
                {
                    AchievementId = 1,
                    Title = "First Workout",
                    Description = "Complete your first workout",
                    Criteria = "1 workout",
                    IsUnlocked = true
                },
                new AchievementShowcaseItem
                {
                    AchievementId = 2,
                    Title = "10 Workouts",
                    Description = "Complete 10 workouts",
                    Criteria = "10 workouts",
                    IsUnlocked = false
                }
            };

            _achievementsRepository.GetAchievementShowcaseForClient(clientId).Returns(mockShowcaseItems);

            // Act
            _sut.LoadAchievementsCommand.Execute(clientId);

            // Assert
            _sut.IsLoading.Should().BeFalse(); // Returns to false in the finally block
            _sut.Achievements.Should().HaveCount(2);

            var firstAchievement = _sut.Achievements.First();
            firstAchievement.AchievementId.Should().Be(1);
            firstAchievement.Name.Should().Be("First Workout");
            firstAchievement.Description.Should().Be("Complete your first workout");
            firstAchievement.Criteria.Should().Be("1 workout");
            firstAchievement.IsUnlocked.Should().BeTrue();
            firstAchievement.Icon.Should().Be("&#xE73E;");

            var secondAchievement = _sut.Achievements.Last();
            secondAchievement.AchievementId.Should().Be(2);
            secondAchievement.Name.Should().Be("10 Workouts");
            secondAchievement.Description.Should().Be("Complete 10 workouts");
            secondAchievement.Criteria.Should().Be("10 workouts");
            secondAchievement.IsUnlocked.Should().BeFalse();
            secondAchievement.Icon.Should().Be("&#xE72E;");
        }

        [Fact]
        public void LoadAchievementsCommand_RepositoryThrowsException_ClearsAchievementsAndSetsLoadingToFalse()
        {
            // Arrange
            var clientId = 1;
            _sut.Achievements.Add(new Achievement { AchievementId = 99 }); // Pre-populate to ensure it gets cleared

            _achievementsRepository.When(x => x.GetAchievementShowcaseForClient(clientId))
                .Do(x => { throw new Exception("Database error"); });

            // Act
            var exception = Record.Exception(() => _sut.LoadAchievementsCommand.Execute(clientId));

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<Exception>();
            _sut.Achievements.Should().BeEmpty(); // It clears before the exception is thrown
            _sut.IsLoading.Should().BeFalse(); // Finally block should execute
        }
    }
}
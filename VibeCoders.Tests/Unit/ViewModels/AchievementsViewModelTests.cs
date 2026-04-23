using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class AchievementsViewModelTests
    {
        private readonly IClientService clientService;
        private readonly AchievementsViewModel achievementsViewModel;

        private const int ValidClientId = 1;
        private const int ExistingAchievementId = 99;
        private const int FirstAchievementId = 1;
        private const int SecondAchievementId = 2;
        private const string FirstAchievementName = "First Workout";
        private const string FirstAchievementDescription = "Complete your first workout";
        private const string FirstAchievementCriteria = "1 workout";
        private const string FirstAchievementIcon = "&#xE73E;";
        private const string SecondAchievementName = "10 Workouts";
        private const string SecondAchievementDescription = "Complete 10 workouts";
        private const string SecondAchievementCriteria = "10 workouts";
        private const string SecondAchievementIcon = "&#xE72E;";
        private const string DatabaseErrorMessage = "Database error";

        private readonly List<Achievement> mockAchievements;

        public AchievementsViewModelTests()
        {
            clientService = Substitute.For<IClientService>();
            achievementsViewModel = new AchievementsViewModel(clientService);

            mockAchievements = new List<Achievement>
            {
                new Achievement
                {
                    AchievementId = FirstAchievementId,
                    Name = FirstAchievementName,
                    Description = FirstAchievementDescription,
                    Criteria = FirstAchievementCriteria,
                    IsUnlocked = true,
                    Icon = FirstAchievementIcon
                },
                new Achievement
                {
                    AchievementId = SecondAchievementId,
                    Name = SecondAchievementName,
                    Description = SecondAchievementDescription,
                    Criteria = SecondAchievementCriteria,
                    IsUnlocked = false,
                    Icon = SecondAchievementIcon
                }
            };
        }

        [Fact]
        public void LoadAchievementsCommand_ValidClientId_PopulatesAchievementsAndUpdatesLoadingState()
        {
            clientService.GetAchievements(ValidClientId).Returns(mockAchievements);

            achievementsViewModel.LoadAchievementsCommand.Execute(ValidClientId);

            achievementsViewModel.IsLoading.Should().BeFalse();
            achievementsViewModel.Achievements.Should().HaveCount(mockAchievements.Count);

            var firstAchievement = achievementsViewModel.Achievements.First();
            firstAchievement.AchievementId.Should().Be(FirstAchievementId);
            firstAchievement.Name.Should().Be(FirstAchievementName);
            firstAchievement.Description.Should().Be(FirstAchievementDescription);
            firstAchievement.Criteria.Should().Be(FirstAchievementCriteria);
            firstAchievement.IsUnlocked.Should().BeTrue();
            firstAchievement.Icon.Should().Be(FirstAchievementIcon);

            var secondAchievement = achievementsViewModel.Achievements.Last();
            secondAchievement.AchievementId.Should().Be(SecondAchievementId);
            secondAchievement.Name.Should().Be(SecondAchievementName);
            secondAchievement.Description.Should().Be(SecondAchievementDescription);
            secondAchievement.Criteria.Should().Be(SecondAchievementCriteria);
            secondAchievement.IsUnlocked.Should().BeFalse();
            secondAchievement.Icon.Should().Be(SecondAchievementIcon);
        }

        [Fact]
        public void LoadAchievementsCommand_RepositoryThrowsException_ClearsAchievementsAndSetsLoadingToFalse()
        {
            achievementsViewModel.Achievements.Add(new Achievement { AchievementId = ExistingAchievementId });

            clientService.When(x => x.GetAchievements(ValidClientId))
                .Do(x => { throw new Exception(DatabaseErrorMessage); });

            var exception = Record.Exception(() => achievementsViewModel.LoadAchievementsCommand.Execute(ValidClientId));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<Exception>();
            achievementsViewModel.Achievements.Should().BeEmpty();
            achievementsViewModel.IsLoading.Should().BeFalse();
        }
    }
}
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
        private readonly IClientService _clientService;
        private readonly AchievementsViewModel _achievementsViewModel;

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

        private readonly List<Achievement> _mockAchievements;

        public AchievementsViewModelTests()
        {
            _clientService = Substitute.For<IClientService>();
            _achievementsViewModel = new AchievementsViewModel(_clientService);

            _mockAchievements = new List<Achievement>
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
            _clientService.GetAchievements(ValidClientId).Returns(_mockAchievements);

            _achievementsViewModel.LoadAchievementsCommand.Execute(ValidClientId);

            _achievementsViewModel.IsLoading.Should().BeFalse();
            _achievementsViewModel.Achievements.Should().HaveCount(_mockAchievements.Count);

            var firstAchievement = _achievementsViewModel.Achievements.First();
            firstAchievement.AchievementId.Should().Be(FirstAchievementId);
            firstAchievement.Name.Should().Be(FirstAchievementName);
            firstAchievement.Description.Should().Be(FirstAchievementDescription);
            firstAchievement.Criteria.Should().Be(FirstAchievementCriteria);
            firstAchievement.IsUnlocked.Should().BeTrue();
            firstAchievement.Icon.Should().Be(FirstAchievementIcon);

            var secondAchievement = _achievementsViewModel.Achievements.Last();
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
            _achievementsViewModel.Achievements.Add(new Achievement { AchievementId = ExistingAchievementId });

            _clientService.When(x => x.GetAchievements(ValidClientId))
                .Do(x => { throw new Exception(DatabaseErrorMessage); });

            var exception = Record.Exception(() => _achievementsViewModel.LoadAchievementsCommand.Execute(ValidClientId));

            exception.Should().NotBeNull();
            exception.Should().BeOfType<Exception>();
            _achievementsViewModel.Achievements.Should().BeEmpty();
            _achievementsViewModel.IsLoading.Should().BeFalse();
        }
    }
}
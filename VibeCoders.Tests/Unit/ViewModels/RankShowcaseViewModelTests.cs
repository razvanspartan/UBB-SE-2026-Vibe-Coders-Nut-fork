using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class RankShowcaseViewModelTests
    {
        private const int DefaultClientId = 7;
        private const int ExpectedEvaluationCallCount = 1;
        private const string LoadFailedMessage = "load failed";

        private readonly IEvaluationEngine evaluationEngineMock;
        private readonly IUserSession userSessionMock;
        private readonly RankShowcaseViewModel systemUnderTest;

        public RankShowcaseViewModelTests()
        {
            this.evaluationEngineMock = Substitute.For<IEvaluationEngine>();
            this.userSessionMock = Substitute.For<IUserSession>();
            this.systemUnderTest = new RankShowcaseViewModel(this.evaluationEngineMock, this.userSessionMock);
        }

        [Fact]
        public async Task LoadAsync_WhenSnapshotIsAvailable_UpdatesStateAndReplacesShowcaseAchievements()
        {
            var existingAchievementShowcaseItem = RankShowcaseDataFactory.CreateExistingAchievementShowcaseItem();
            var consistencyAchievementShowcaseItem = RankShowcaseDataFactory.CreateUnlockedConsistencyAchievementShowcaseItem();
            var momentumAchievementShowcaseItem = RankShowcaseDataFactory.CreateLockedMomentumAchievementShowcaseItem();
            var rankShowcaseSnapshot = RankShowcaseDataFactory.CreateRankShowcaseSnapshot(
                consistencyAchievementShowcaseItem,
                momentumAchievementShowcaseItem);
            var expectedViewModelState = CreateExpectedState(rankShowcaseSnapshot);

            this.userSessionMock.CurrentClientId.Returns(DefaultClientId);
            this.evaluationEngineMock.BuildRankShowcase(DefaultClientId).Returns(rankShowcaseSnapshot);
            this.systemUnderTest.ShowcaseAchievements.Add(existingAchievementShowcaseItem);

            await this.systemUnderTest.LoadAsync();

            var actualViewModelState = CreateCurrentState();

            object.Equals(actualViewModelState, expectedViewModelState).Should().BeTrue();
            this.systemUnderTest.ShowcaseAchievements.Should().Equal(consistencyAchievementShowcaseItem, momentumAchievementShowcaseItem);
            this.systemUnderTest.ShowcaseAchievements.Should().NotContain(existingAchievementShowcaseItem);
            this.evaluationEngineMock.Received(ExpectedEvaluationCallCount).BuildRankShowcase(DefaultClientId);
        }

        [Fact]
        public async Task LoadAsync_WhenEvaluationEngineThrows_LeavesLoadingStateFalse()
        {
            var expectedViewModelState = CreateCurrentState();

            this.userSessionMock.CurrentClientId.Returns(DefaultClientId);
            this.evaluationEngineMock
                .BuildRankShowcase(DefaultClientId)
                .Returns(_ => throw new InvalidOperationException(LoadFailedMessage));

            Func<Task> loadRankShowcaseAsync = () => this.systemUnderTest.LoadAsync();

            await loadRankShowcaseAsync.Should().ThrowAsync<InvalidOperationException>();

            var actualViewModelState = CreateCurrentState();

            object.Equals(actualViewModelState, expectedViewModelState).Should().BeTrue();
        }

        private RankShowcaseViewModelState CreateCurrentState()
        {
            return new RankShowcaseViewModelState(
                this.systemUnderTest.DisplayLevel,
                this.systemUnderTest.RankTitle,
                this.systemUnderTest.UnlockedAchievementsDisplay,
                this.systemUnderTest.LevelDisplayLine,
                this.systemUnderTest.ProgressPercent,
                this.systemUnderTest.NextRankInfo,
                this.systemUnderTest.HasNextRank,
                this.systemUnderTest.IsLoading,
                this.systemUnderTest.ShowcaseAchievements.Count);
        }

        private static RankShowcaseViewModelState CreateExpectedState(RankShowcaseSnapshot rankShowcaseSnapshot)
        {
            return new RankShowcaseViewModelState(
                rankShowcaseSnapshot.DisplayLevel,
                rankShowcaseSnapshot.RankTitle,
                rankShowcaseSnapshot.UnlockedAchievementsDisplay,
                rankShowcaseSnapshot.LevelDisplayLine,
                rankShowcaseSnapshot.ProgressPercent,
                rankShowcaseSnapshot.NextRankInfo,
                rankShowcaseSnapshot.HasNextRank,
                false,
                rankShowcaseSnapshot.ShowcaseAchievements.Count);
        }

        private sealed record RankShowcaseViewModelState(
            int DisplayLevel,
            string RankTitle,
            string UnlockedAchievementsDisplay,
            string LevelDisplayLine,
            double ProgressPercent,
            string NextRankInfo,
            bool HasNextRank,
            bool IsLoading,
            int ShowcaseAchievementCount);
    }
}
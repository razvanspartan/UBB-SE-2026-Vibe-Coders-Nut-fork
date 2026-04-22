using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class ClientDashboardViewModelTests
    {
        private readonly IWorkoutAnalyticsStore storeMock;
        private readonly IRepositoryAchievements achievementsMock;
        private readonly IRepositoryNutrition nutritionMock;
        private readonly IUserSession sessionMock;
        private readonly IAnalyticsDashboardRefreshBus refreshBusMock;
        private readonly ClientDashboardViewModel systemUnderTest;

        public ClientDashboardViewModelTests()
        {
            this.storeMock = Substitute.For<IWorkoutAnalyticsStore>();
            this.achievementsMock = Substitute.For<IRepositoryAchievements>();
            this.nutritionMock = Substitute.For<IRepositoryNutrition>();
            this.sessionMock = Substitute.For<IUserSession>();
            this.refreshBusMock = Substitute.For<IAnalyticsDashboardRefreshBus>();

            this.sessionMock.CurrentClientId.Returns(1);
            this.achievementsMock.GetAchievementShowcaseForClient(1).Returns(new List<AchievementShowcaseItem>());

            this.systemUnderTest = new ClientDashboardViewModel(
                this.storeMock,
                this.sessionMock,
                this.refreshBusMock,
                this.achievementsMock,
                this.nutritionMock);
        }

        [Fact]
        public async Task LoadInitialAsync_PopulatesDataSuccessfully()
        {
            var summary = new DashboardSummary { TotalWorkouts = 10, TotalActiveTimeLastSevenDays = TimeSpan.FromHours(2), PreferredWorkoutName = "Push" };
            var buckets = new List<ConsistencyWeekBucket> { new ConsistencyWeekBucket { WeekStart = DateOnly.FromDateTime(DateTime.Today), WorkoutCount = 3 } };
            var historyPage = new WorkoutHistoryPageResult { TotalCount = 10, Items = new List<WorkoutHistoryRow> { new WorkoutHistoryRow { Id = 1, LogDate = DateTime.Today, WorkoutName = "Pull" } } };
            
            this.storeMock.GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(summary));
            this.storeMock.GetConsistencyLastFourWeeksAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ConsistencyWeekBucket>>(buckets));
            this.storeMock.GetWorkoutHistoryPageAsync(1, 0, ClientDashboardViewModel.DefaultPageSize, Arg.Any<CancellationToken>()).Returns(Task.FromResult(historyPage));
            
            this.achievementsMock.GetAchievementShowcaseForClient(1).Returns(new List<AchievementShowcaseItem>
            {
                new AchievementShowcaseItem { IsUnlocked = true, AchievementId = 1 }
            });

            await this.systemUnderTest.LoadInitialAsync();

            this.systemUnderTest.TotalWorkouts.Should().Be(10);
            this.systemUnderTest.PreferredWorkoutDisplay.Should().Be("Push");
            this.systemUnderTest.ChartSeries.Should().NotBeEmpty();
            this.systemUnderTest.HistoryItems.Should().HaveCount(1);
            this.systemUnderTest.RecentAchievements.Should().HaveCount(1);
            this.systemUnderTest.IsLoadingSummary.Should().BeFalse();
            this.systemUnderTest.ShowEmptyState.Should().BeFalse();
            this.systemUnderTest.PageDisplayText.Should().Be("Page 1 of 2");
        }

        [Fact]
        public void DefaultProperties_SetCorrectly()
        {
            this.systemUnderTest.PageDisplayText.Should().BeEmpty();
            this.systemUnderTest.RecentAchievements.Should().BeEmpty();
            this.systemUnderTest.HistoryItems.Should().BeEmpty();
            this.systemUnderTest.PreferredWorkoutDisplay.Should().Be("-");
            this.systemUnderTest.ShowEmptyState.Should().BeTrue();
        }

        [Fact]
        public async Task NextPageAsync_WhenCanGoNextIsTrue_IncrementsPageAndLoadsHistory()
        {
            var historyPage1 = new WorkoutHistoryPageResult { TotalCount = 10, Items = new List<WorkoutHistoryRow>() };
            this.storeMock.GetWorkoutHistoryPageAsync(1, 0, ClientDashboardViewModel.DefaultPageSize, Arg.Any<CancellationToken>()).Returns(Task.FromResult(historyPage1));
            
            var summary = new DashboardSummary();
            this.storeMock.GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(summary));
            this.storeMock.GetConsistencyLastFourWeeksAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ConsistencyWeekBucket>>(new List<ConsistencyWeekBucket>()));
            
            await this.systemUnderTest.LoadInitialAsync();

            this.systemUnderTest.CanGoNext.Should().BeTrue();

            var historyPage2 = new WorkoutHistoryPageResult { TotalCount = 10, Items = new List<WorkoutHistoryRow>() };
            this.storeMock.GetWorkoutHistoryPageAsync(1, 1, ClientDashboardViewModel.DefaultPageSize, Arg.Any<CancellationToken>()).Returns(Task.FromResult(historyPage2));

            await this.systemUnderTest.NextPageCommand.ExecuteAsync(null);

            this.systemUnderTest.CurrentPage.Should().Be(1);
            this.systemUnderTest.PageDisplayText.Should().Be("Page 2 of 2");
            this.systemUnderTest.CanGoPrevious.Should().BeTrue();
            this.systemUnderTest.CanGoNext.Should().BeFalse();
        }

        [Fact]
        public async Task PreviousPageAsync_WhenCanGoPreviousIsTrue_DecrementsPageAndLoadsHistory()
        {
            var historyPage = new WorkoutHistoryPageResult { TotalCount = 10, Items = new List<WorkoutHistoryRow>() };
            this.storeMock.GetWorkoutHistoryPageAsync(1, Arg.Any<int>(), ClientDashboardViewModel.DefaultPageSize, Arg.Any<CancellationToken>()).Returns(Task.FromResult(historyPage));
            
            this.storeMock.GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new DashboardSummary()));
            this.storeMock.GetConsistencyLastFourWeeksAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ConsistencyWeekBucket>>(new List<ConsistencyWeekBucket>()));
            
            await this.systemUnderTest.LoadInitialAsync();
            await this.systemUnderTest.NextPageCommand.ExecuteAsync(null);

            this.systemUnderTest.CurrentPage.Should().Be(1);

            await this.systemUnderTest.PreviousPageCommand.ExecuteAsync(null);

            this.systemUnderTest.CurrentPage.Should().Be(0);
            this.systemUnderTest.CanGoPrevious.Should().BeFalse();
        }

        [Fact]
        public async Task PreviousPageAsync_WhenCanGoPreviousIsFalse_DoesNothing()
        {
            this.systemUnderTest.CurrentPage = 0;
            this.systemUnderTest.TotalCount = 10;
            this.systemUnderTest.CanGoPrevious = false;

            await this.systemUnderTest.PreviousPageCommand.ExecuteAsync(null);

            this.systemUnderTest.CurrentPage.Should().Be(0);
            await this.storeMock.DidNotReceive().GetWorkoutHistoryPageAsync(default, default, default, default);
        }

        [Fact]
        public async Task NextPageAsync_WhenCanGoNextIsFalse_DoesNothing()
        {
            this.systemUnderTest.CurrentPage = 1;
            this.systemUnderTest.TotalCount = 10;

            await this.systemUnderTest.NextPageCommand.ExecuteAsync(null);

            this.systemUnderTest.CurrentPage.Should().Be(1);
            await this.storeMock.DidNotReceive().GetWorkoutHistoryPageAsync(default, default, default, default);
        }

        [Fact]
        public void ReloadAchievementsPreview_ReloadsAchievementsCorrectly()
        {
            this.achievementsMock.GetAchievementShowcaseForClient(1).Returns(new List<AchievementShowcaseItem>
            {
                new AchievementShowcaseItem { IsUnlocked = true, AchievementId = 1 },
                new AchievementShowcaseItem { IsUnlocked = false, AchievementId = 2 }
            });

            this.systemUnderTest.ReloadAchievementsPreview();

            this.systemUnderTest.RecentAchievements.Should().HaveCount(1);
        }
        
        [Fact]
        public async Task RefreshRequestedEvent_TriggersLoadAllAsync()
        {
            this.storeMock.GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new DashboardSummary()));
            this.storeMock.GetConsistencyLastFourWeeksAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ConsistencyWeekBucket>>(new List<ConsistencyWeekBucket>()));
            this.storeMock.GetWorkoutHistoryPageAsync(1, 0, ClientDashboardViewModel.DefaultPageSize, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new WorkoutHistoryPageResult()));
            
            this.refreshBusMock.RefreshRequested += Raise.EventWith(this, EventArgs.Empty);

            await Task.Delay(100);

            await this.storeMock.Received(1).GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RefreshCommand_TriggersLoadAllAsync()
        {
            this.storeMock.GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new DashboardSummary()));
            this.storeMock.GetConsistencyLastFourWeeksAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ConsistencyWeekBucket>>(new List<ConsistencyWeekBucket>()));
            this.storeMock.GetWorkoutHistoryPageAsync(1, 0, ClientDashboardViewModel.DefaultPageSize, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new WorkoutHistoryPageResult()));

            await this.systemUnderTest.RefreshCommand.ExecuteAsync(null);

            await this.storeMock.Received(1).GetDashboardSummaryAsync(1, Arg.Any<CancellationToken>());
        }
    }
}


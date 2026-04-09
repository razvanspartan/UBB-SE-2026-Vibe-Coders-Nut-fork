namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using LiveChartsCore;
    using LiveChartsCore.SkiaSharpView;
    using LiveChartsCore.SkiaSharpView.Painting;
    using SkiaSharp;
    using VibeCoders.Domain;
    using VibeCoders.Models;
    using VibeCoders.Models.Analytics;
    using VibeCoders.Services;

    /// <summary>
    /// ViewModel for the client dashboard, providing analytics, history, and achievement overviews.
    /// </summary>
    public sealed partial class ClientDashboardViewModel : ObservableObject
    {
        /// <summary>
        /// The default number of items per page for workout history.
        /// </summary>
        public const int DefaultPageSize = 5;

        private const int MaximumDisplayAchievements = 3;
        private const int DefaultGeometrySize = 12;
        private const int DefaultStrokeThickness = 3;
        private const float GradientMidpointX = 0.5f;
        private const float GradientStartY = 0;
        private const float GradientEndY = 1;

        private static readonly SKColor PrimaryBlueColor = new SKColor(0x00, 0x5F, 0xB8);
        private static readonly SKColor PrimaryBlueTransparentColor = new SKColor(0x00, 0x5F, 0xB8, 90);
        private static readonly SKColor PrimaryBlueZeroAlphaColor = new SKColor(0x00, 0x5F, 0xB8, 0);
        private static readonly SKColor WhiteColor = new SKColor(0xFF, 0xFF, 0xFF);
        private static readonly SKColor GrayColor = new SKColor(0x8A, 0x8A, 0x8A);

        private readonly IWorkoutAnalyticsStore store;
        private readonly IDataStorage dataStorage;
        private readonly IUserSession session;
        private readonly IAnalyticsDashboardRefreshBus refreshBus;

        private CancellationTokenSource? loadCancellationTokenSource;

        [ObservableProperty]
        private int totalWorkouts;

        [ObservableProperty]
        private string activeTimeSevenDaysDisplay = "0:00:00";

        [ObservableProperty]
        private string preferredWorkoutDisplay = "-";

        [ObservableProperty]
        private ObservableCollection<ConsistencyWeekBucket> consistencyBuckets = new ObservableCollection<ConsistencyWeekBucket>();

        [ObservableProperty]
        private ISeries[] chartSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] chartXAxes = new[] { new Axis() };

        [ObservableProperty]
        private int currentPage;

        [ObservableProperty]
        private int totalCount;

        [ObservableProperty]
        private bool canGoPrevious;

        [ObservableProperty]
        private bool canGoNext;

        [ObservableProperty]
        private bool isLoadingSummary;

        [ObservableProperty]
        private bool isLoadingHistory;

        [ObservableProperty]
        private bool isLoadingChart;

        [ObservableProperty]
        private bool showEmptyState = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDashboardViewModel"/> class.
        /// </summary>
        /// <param name="store">The workout analytics store.</param>
        /// <param name="dataStorage">The data storage service.</param>
        /// <param name="session">The user session service.</param>
        /// <param name="refreshBus">The analytics dashboard refresh bus.</param>
        public ClientDashboardViewModel(
            IWorkoutAnalyticsStore store,
            IDataStorage dataStorage,
            IUserSession session,
            IAnalyticsDashboardRefreshBus refreshBus)
        {
            this.store = store;
            this.dataStorage = dataStorage;
            this.session = session;
            this.refreshBus = refreshBus;
            this.refreshBus.RefreshRequested += this.OnRefreshRequested;
        }

        /// <summary>
        /// Gets the collection of workout history items.
        /// </summary>
        public ObservableCollection<WorkoutHistoryItemViewModel> HistoryItems { get; } = new ObservableCollection<WorkoutHistoryItemViewModel>();

        /// <summary>
        /// Gets the collection of recent achievements.
        /// </summary>
        public ObservableCollection<AchievementShowcaseItem> RecentAchievements { get; } = new ObservableCollection<AchievementShowcaseItem>();

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = ClientDashboardViewModel.DefaultPageSize;

        /// <summary>
        /// Gets the text displaying the current page and total pages.
        /// </summary>
        public string PageDisplayText =>
            this.TotalPages == 0
                ? string.Empty
                : string.Create(CultureInfo.InvariantCulture, $"Page {this.CurrentPage + 1} of {this.TotalPages}");

        private int TotalPages =>
            this.TotalCount == 0 ? 0 : (this.TotalCount + this.PageSize - 1) / this.PageSize;

        /// <summary>
        /// Loads the initial data for the dashboard asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task LoadInitialAsync() => this.LoadAllAsync();

        /// <summary>
        /// Reloads the achievements preview.
        /// </summary>
        public void ReloadAchievementsPreview() =>
            this.LoadRecentAchievements((int)this.session.CurrentClientId);

        partial void OnCurrentPageChanged(int value) => this.OnPropertyChanged(nameof(this.PageDisplayText));

        partial void OnTotalCountChanged(int value) => this.OnPropertyChanged(nameof(this.PageDisplayText));

        [RelayCommand]
        private Task RefreshAsync() => this.LoadAllAsync();

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (!this.CanGoNext)
            {
                return;
            }

            this.CurrentPage++;
            await this.LoadHistoryPageAsync().ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (!this.CanGoPrevious)
            {
                return;
            }

            this.CurrentPage--;
            await this.LoadHistoryPageAsync().ConfigureAwait(true);
        }

        private void OnRefreshRequested(object? sender, EventArgs eventArgs) => _ = this.LoadAllAsync();

        private async Task LoadAllAsync()
        {
            this.CancelPendingLoad();
            var cancellationToken = this.loadCancellationTokenSource!.Token;
            var clientId = this.session.CurrentClientId;

            try
            {
                this.IsLoadingSummary = true;
                this.IsLoadingChart = true;
                this.IsLoadingHistory = true;

                await this.store.EnsureCreatedAsync(cancellationToken).ConfigureAwait(true);

                var summaryTask = this.store.GetDashboardSummaryAsync(clientId, cancellationToken);
                var bucketsTask = this.store.GetConsistencyLastFourWeeksAsync(clientId, cancellationToken);
                this.CurrentPage = 0;
                var historyTask = this.store.GetWorkoutHistoryPageAsync(clientId, this.CurrentPage, this.PageSize, cancellationToken);

                await Task.WhenAll(summaryTask, bucketsTask, historyTask).ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();

                this.ApplySummary(summaryTask.Result);
                this.ApplyBuckets(bucketsTask.Result);
                this.ApplyHistory(historyTask.Result, clientId);
                this.LoadRecentAchievements((int)clientId);

                this.IsLoadingSummary = false;
                this.IsLoadingChart = false;
                this.IsLoadingHistory = false;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                this.IsLoadingSummary = false;
                this.IsLoadingChart = false;
                this.IsLoadingHistory = false;
            }
        }

        private async Task LoadHistoryPageAsync()
        {
            this.CancelPendingLoad();
            var cancellationToken = this.loadCancellationTokenSource!.Token;
            this.IsLoadingHistory = true;

            try
            {
                await this.store.EnsureCreatedAsync(cancellationToken).ConfigureAwait(true);
                var result = await this.store.GetWorkoutHistoryPageAsync(
                    this.session.CurrentClientId, this.CurrentPage, this.PageSize, cancellationToken).ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();
                this.ApplyHistory(result, this.session.CurrentClientId);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                this.IsLoadingHistory = false;
            }
        }

        private void LoadRecentAchievements(int clientId)
        {
            this.RecentAchievements.Clear();

            var achievementItems = this.dataStorage.GetAchievementShowcaseForClient(clientId)
                .Where(achievement => achievement.IsUnlocked)
                .OrderByDescending(achievement => achievement.AchievementId)
                .Take(ClientDashboardViewModel.MaximumDisplayAchievements);

            foreach (var achievementItem in achievementItems)
            {
                this.RecentAchievements.Add(achievementItem);
            }
        }

        private void CancelPendingLoad()
        {
            this.loadCancellationTokenSource?.Cancel();
            this.loadCancellationTokenSource?.Dispose();
            this.loadCancellationTokenSource = new CancellationTokenSource();
        }

        private void ApplySummary(DashboardSummary summary)
        {
            this.TotalWorkouts = summary.TotalWorkouts;
            this.ActiveTimeSevenDaysDisplay =
                ActiveTimeFormatter.ToHourMinuteSecond(summary.TotalActiveTimeLastSevenDays);
            this.PreferredWorkoutDisplay = string.IsNullOrWhiteSpace(summary.PreferredWorkoutName)
                ? "-"
                : summary.PreferredWorkoutName;
        }

        private void ApplyBuckets(IReadOnlyList<ConsistencyWeekBucket> buckets)
        {
            this.ConsistencyBuckets.Clear();
            foreach (var bucketItem in buckets)
            {
                this.ConsistencyBuckets.Add(bucketItem);
            }

            this.ChartSeries = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = buckets.Select(bucketItem => bucketItem.WorkoutCount).ToArray(),
                    Name = "Workouts",
                    GeometrySize = ClientDashboardViewModel.DefaultGeometrySize,
                    Stroke = new SolidColorPaint(ClientDashboardViewModel.PrimaryBlueColor) { StrokeThickness = ClientDashboardViewModel.DefaultStrokeThickness },
                    GeometryStroke = new SolidColorPaint(ClientDashboardViewModel.PrimaryBlueColor) { StrokeThickness = ClientDashboardViewModel.DefaultStrokeThickness },
                    GeometryFill = new SolidColorPaint(ClientDashboardViewModel.WhiteColor),
                    Fill = new LinearGradientPaint(
                        new[] { ClientDashboardViewModel.PrimaryBlueTransparentColor, ClientDashboardViewModel.PrimaryBlueZeroAlphaColor },
                        new SKPoint(ClientDashboardViewModel.GradientMidpointX, ClientDashboardViewModel.GradientStartY),
                        new SKPoint(ClientDashboardViewModel.GradientMidpointX, ClientDashboardViewModel.GradientEndY))
                }
            };

            this.ChartXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = buckets.Select(bucketItem =>
                        bucketItem.WeekStart.ToString("MMM dd", CultureInfo.InvariantCulture)).ToArray(),
                    LabelsRotation = 0,
                    TextSize = ClientDashboardViewModel.DefaultGeometrySize,
                    LabelsPaint = new SolidColorPaint(ClientDashboardViewModel.GrayColor)
                }
            };
        }

        private void ApplyHistory(WorkoutHistoryPageResult result, long clientId)
        {
            this.TotalCount = result.TotalCount;
            this.ShowEmptyState = result.TotalCount == 0;
            this.UpdatePaginationButtons();

            this.HistoryItems.Clear();
            foreach (var workoutHistoryRow in result.Items)
            {
                this.HistoryItems.Add(new WorkoutHistoryItemViewModel(this.store, clientId, workoutHistoryRow));
            }
        }

        private void UpdatePaginationButtons()
        {
            var pages = this.TotalPages;
            this.CanGoPrevious = this.CurrentPage > 0 && pages > 0;
            this.CanGoNext = pages > 0 && this.CurrentPage < pages - 1;
        }
    }
}
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public sealed partial class ClientDashboardViewModel : ObservableObject
{
    public const int DefaultPageSize = 5;

    private readonly IWorkoutAnalyticsStore store;
    private readonly IUserSession session;
    private readonly IAnalyticsDashboardRefreshBus refreshBus;
    private CancellationTokenSource? loadCts;
    private readonly IRepositoryAchievements achievementsRepository;
    private readonly IRepositoryNutrition nutritionRepository;
    public ClientDashboardViewModel(
        IWorkoutAnalyticsStore store,
        IUserSession session,
        IAnalyticsDashboardRefreshBus refreshBus,
        IRepositoryAchievements achievementsRepository,
        IRepositoryNutrition nutritionRepository)
    {
        this.store = store;
        this.session = session;
        this.refreshBus = refreshBus;
        this.achievementsRepository = achievementsRepository;
        this.refreshBus.RefreshRequested += OnRefreshRequested;
        this.nutritionRepository = nutritionRepository;
    }

    [ObservableProperty]
    public partial int TotalWorkouts { get; set; }

    [ObservableProperty]
    public partial string ActiveTimeSevenDaysDisplay { get; set; } = "0:00:00";

    [ObservableProperty]
    public partial string PreferredWorkoutDisplay { get; set; } = "-";

    private ObservableCollection<ConsistencyWeekBucket> consistencyBuckets = new ();

    [ObservableProperty]
    public partial ISeries[] ChartSeries { get; set; } = Array.Empty<ISeries>();

    [ObservableProperty]
    public partial Axis[] ChartXAxes { get; set; } = new[] { new Axis() };

    [ObservableProperty]
    public partial int CurrentPage { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial bool CanGoPrevious { get; set; }

    [ObservableProperty]
    public partial bool CanGoNext { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingSummary { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingHistory { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingChart { get; set; }

    [ObservableProperty]
    public partial bool ShowEmptyState { get; set; } = true;

    public ObservableCollection<WorkoutHistoryItemViewModel> HistoryItems { get; } = new ();

    public ObservableCollection<AchievementShowcaseItem> RecentAchievements { get; } = new ();

    public int PageSize { get; set; } = DefaultPageSize;

    private int TotalPages =>
        TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

    public string PageDisplayText =>
        TotalPages == 0
            ? string.Empty
            : string.Create(CultureInfo.InvariantCulture, $"Page {CurrentPage + 1} of {TotalPages}");

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageDisplayText));
    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(PageDisplayText));

    [RelayCommand]
    private Task RefreshAsync() => LoadAllAsync();

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!this.CanGoNext)
        {
            return;
        }
        this.CurrentPage++;
        await LoadHistoryPageAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!this.CanGoPrevious)
        {
            return;
        }
        this.CurrentPage--;
        await LoadHistoryPageAsync().ConfigureAwait(true);
    }

    public Task LoadInitialAsync() => LoadAllAsync();

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAllAsync();

    private async Task LoadAllAsync()
    {
        CancelPendingLoad();
        var token = this.loadCts!.Token;
        var clientId = this.session.CurrentClientId;

        try
        {
            IsLoadingSummary = true;
            IsLoadingChart = true;
            IsLoadingHistory = true;

            await this.store.EnsureCreatedAsync(token).ConfigureAwait(true);

            var summaryTask = this.store.GetDashboardSummaryAsync(clientId, token);
            var bucketsTask = this.store.GetConsistencyLastFourWeeksAsync(clientId, token);
            CurrentPage = 0;
            var historyTask = this.store.GetWorkoutHistoryPageAsync(clientId, CurrentPage, PageSize, token);

            await Task.WhenAll(summaryTask, bucketsTask, historyTask).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();

            ApplySummary(summaryTask.Result);
            ApplyBuckets(bucketsTask.Result);
            ApplyHistory(historyTask.Result, clientId);
            LoadRecentAchievements((int)clientId);

            IsLoadingSummary = false;
            IsLoadingChart = false;
            IsLoadingHistory = false;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsLoadingSummary = false;
            IsLoadingChart = false;
            IsLoadingHistory = false;
        }
    }

    private async Task LoadHistoryPageAsync()
    {
        CancelPendingLoad();
        var token = this.loadCts!.Token;
        IsLoadingHistory = true;

        try
        {
            await this.store.EnsureCreatedAsync(token).ConfigureAwait(true);
            var result = await this.store.GetWorkoutHistoryPageAsync(
                this.session.CurrentClientId, CurrentPage, PageSize, token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            ApplyHistory(result, this.session.CurrentClientId);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsLoadingHistory = false;
        }
    }
    public void ReloadAchievementsPreview() =>
        LoadRecentAchievements((int)this.session.CurrentClientId);

    private void LoadRecentAchievements(int clientId)
    {
        RecentAchievements.Clear();

        var items = this.achievementsRepository.GetAchievementShowcaseForClient(clientId)
            .Where(a => a.IsUnlocked)
            .OrderByDescending(a => a.AchievementId)
            .Take(3);

        foreach (var item in items)
        {
            RecentAchievements.Add(item);
        }
    }

    private void CancelPendingLoad()
    {
        this.loadCts?.Cancel();
        this.loadCts?.Dispose();
        this.loadCts = new CancellationTokenSource();
    }

    private void ApplySummary(DashboardSummary summary)
    {
        TotalWorkouts = summary.TotalWorkouts;
        ActiveTimeSevenDaysDisplay =
            ActiveTimeFormatter.ToHourMinuteSecond(summary.TotalActiveTimeLastSevenDays);
        PreferredWorkoutDisplay = string.IsNullOrWhiteSpace(summary.PreferredWorkoutName)
            ? "-"
            : summary.PreferredWorkoutName;
    }

    private void ApplyBuckets(IReadOnlyList<ConsistencyWeekBucket> buckets)
    {
        this.consistencyBuckets.Clear();
        foreach (var b in buckets)
        {
            consistencyBuckets.Add(b);
        }

        this.ChartSeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = buckets.Select(b => b.WorkoutCount).ToArray(),
                Name = "Workouts",
                GeometrySize = 12,
                Stroke = new SolidColorPaint(new SKColor(0x00, 0x5F, 0xB8)) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(new SKColor(0x00, 0x5F, 0xB8)) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(new SKColor(0xFF, 0xFF, 0xFF)),
                Fill = new LinearGradientPaint(
                    new[] { new SKColor(0x00, 0x5F, 0xB8, 90), new SKColor(0x00, 0x5F, 0xB8, 0) },
                    new SKPoint(0.5f, 0),
                    new SKPoint(0.5f, 1))
            }
        };

        this.ChartXAxes = new Axis[]
        {
            new Axis
            {
                Labels = buckets.Select(b =>
                    b.WeekStart.ToString("MMM dd", CultureInfo.InvariantCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(new SKColor(0x8A, 0x8A, 0x8A))
            }
        };
    }

    private void ApplyHistory(WorkoutHistoryPageResult result, long clientId)
    {
        TotalCount = result.TotalCount;
        ShowEmptyState = result.TotalCount == 0;
        UpdatePaginationButtons();

        HistoryItems.Clear();
        foreach (var row in result.Items)
        {
            HistoryItems.Add(new WorkoutHistoryItemViewModel(store, clientId, row));
        }
    }

    private void UpdatePaginationButtons()
    {
        var pages = TotalPages;
        CanGoPrevious = CurrentPage > 0 && pages > 0;
        CanGoNext = pages > 0 && CurrentPage < pages - 1;
    }
}

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

namespace VibeCoders.ViewModels;

public sealed partial class ClientDashboardViewModel : ObservableObject
{
    public const int DefaultPageSize = 8;

    private readonly IWorkoutAnalyticsStore _store;
    private readonly IDataStorage _dataStorage;
    private readonly IUserSession _session;
    private readonly IAnalyticsDashboardRefreshBus _refreshBus;
    private CancellationTokenSource? _loadCts;

    public ClientDashboardViewModel(
        IWorkoutAnalyticsStore store,
        IDataStorage dataStorage,
        IUserSession session,
        IAnalyticsDashboardRefreshBus refreshBus)
    {
        _store = store;
        _dataStorage = dataStorage;
        _session = session;
        _refreshBus = refreshBus;
        _refreshBus.RefreshRequested += OnRefreshRequested;
    }

    [ObservableProperty]
    private ObservableCollection<Achievement> recentAchievements = new();

    [ObservableProperty]
    private int totalWorkouts;

    [ObservableProperty]
    private string activeTimeSevenDaysDisplay = "0:00:00";

    [ObservableProperty]
    private string preferredWorkoutDisplay = "-";

    [ObservableProperty]
    private ObservableCollection<ConsistencyWeekBucket> consistencyBuckets = new();

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

    public ObservableCollection<WorkoutHistoryItemViewModel> HistoryItems { get; } = new();

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
        if (!CanGoNext) return;
        CurrentPage++;
        await LoadHistoryPageAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious) return;
        CurrentPage--;
        await LoadHistoryPageAsync().ConfigureAwait(true);
    }

    public Task LoadInitialAsync() => LoadAllAsync();

    private void OnRefreshRequested(object? sender, EventArgs e) => _ = LoadAllAsync();

    private async Task LoadAllAsync()
    {
        CancelPendingLoad();
        var token = _loadCts!.Token;
        var uid = _session.CurrentUserId;

        try
        {
            IsLoadingSummary = true;
            IsLoadingChart = true;
            IsLoadingHistory = true;

            await _store.EnsureCreatedAsync(token).ConfigureAwait(true);

            var summaryTask = _store.GetDashboardSummaryAsync(uid, token);
            var bucketsTask = _store.GetConsistencyLastFourWeeksAsync(uid, token);
            CurrentPage = 0;
            var historyTask = _store.GetWorkoutHistoryPageAsync(uid, CurrentPage, PageSize, token);

            await Task.WhenAll(summaryTask, bucketsTask, historyTask).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();

            ApplySummary(summaryTask.Result);
            ApplyBuckets(bucketsTask.Result);
            ApplyHistory(historyTask.Result, uid);
            LoadRecentAchievements((int)uid);

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
        var token = _loadCts!.Token;
        IsLoadingHistory = true;

        try
        {
            await _store.EnsureCreatedAsync(token).ConfigureAwait(true);
            var result = await _store.GetWorkoutHistoryPageAsync(
                _session.CurrentUserId, CurrentPage, PageSize, token).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            ApplyHistory(result, _session.CurrentUserId);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsLoadingHistory = false;
        }
    }

    private void LoadRecentAchievements(int clientId)
    {
        RecentAchievements.Clear();

        var showcase = _dataStorage.GetAchievementShowcaseForClient(clientId)
            .Take(5)
            .Select(a => new Achievement
            {
                AchievementId = a.AchievementId,
                Name = a.Title,
                Description = a.Description,
                Criteria = a.Criteria,
                IsUnlocked = a.IsUnlocked,
                Icon = a.IsUnlocked ? "&#xE73E;" : "&#xE72E;"
            });

        foreach (var achievement in showcase)
        {
            RecentAchievements.Add(achievement);
        }
    }

    private void CancelPendingLoad()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
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
        ConsistencyBuckets.Clear();
        foreach (var b in buckets)
        {
            ConsistencyBuckets.Add(b);
        }

        ChartSeries = new ISeries[]
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

        ChartXAxes = new Axis[]
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

    private void ApplyHistory(WorkoutHistoryPageResult result, long userId)
    {
        TotalCount = result.TotalCount;
        ShowEmptyState = result.TotalCount == 0;
        UpdatePaginationButtons();

        HistoryItems.Clear();
        foreach (var row in result.Items)
        {
            HistoryItems.Add(new WorkoutHistoryItemViewModel(_store, userId, row));
        }
    }

    private void UpdatePaginationButtons()
    {
        var pages = TotalPages;
        CanGoPrevious = CurrentPage > 0 && pages > 0;
        CanGoNext = pages > 0 && CurrentPage < pages - 1;
    }
}

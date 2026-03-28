using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Domain;
using VibeCoders.Models.Analytics;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

/// <summary>
/// Drives the client analytics dashboard: summary KPIs, 4-week consistency
/// chart data, and paginated workout history with expandable set detail.
/// </summary>
public sealed partial class ClientDashboardViewModel : ObservableObject
{
    public const int DefaultPageSize = 8;

    private readonly IWorkoutAnalyticsStore _store;
    private readonly IUserSession _session;
    private readonly IAnalyticsDashboardRefreshBus _refreshBus;
    private CancellationTokenSource? _loadCts;

    public ClientDashboardViewModel(
        IWorkoutAnalyticsStore store,
        IUserSession session,
        IAnalyticsDashboardRefreshBus refreshBus)
    {
        _store = store;
        _session = session;
        _refreshBus = refreshBus;
        _refreshBus.RefreshRequested += OnRefreshRequested;
    }

    // -- Summary KPIs --

    [ObservableProperty]
    private int totalWorkouts;

    [ObservableProperty]
    private string activeTimeSevenDaysDisplay = "0:00:00";

    [ObservableProperty]
    private string preferredWorkoutDisplay = "\u2014";

    // -- Consistency chart data --

    [ObservableProperty]
    private ObservableCollection<ConsistencyWeekBucket> consistencyBuckets = new();

    // -- Pagination --

    [ObservableProperty]
    private int currentPage;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private bool canGoPrevious;

    [ObservableProperty]
    private bool canGoNext;

    // -- Loading flags --

    [ObservableProperty]
    private bool isLoadingSummary;

    [ObservableProperty]
    private bool isLoadingHistory;

    [ObservableProperty]
    private bool isLoadingChart;

    // -- Empty state --

    [ObservableProperty]
    private bool showEmptyState = true;

    // -- History items --

    public ObservableCollection<WorkoutHistoryItemViewModel> HistoryItems { get; } = new();

    public int PageSize { get; set; } = DefaultPageSize;

    private int TotalPages =>
        TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

    // -- Commands --

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

    /// <summary>
    /// Entry point called when the dashboard page loads or is navigated to.
    /// </summary>
    public Task LoadInitialAsync() => LoadAllAsync();

    // -- Private loading --

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
            IsLoadingSummary = false;

            ApplyBuckets(bucketsTask.Result);
            IsLoadingChart = false;

            ApplyHistory(historyTask.Result, uid);
            IsLoadingHistory = false;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer load; safe to ignore.
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
        catch (OperationCanceledException) { }
        finally
        {
            IsLoadingHistory = false;
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
            ? "\u2014"
            : summary.PreferredWorkoutName;
    }

    private void ApplyBuckets(IReadOnlyList<ConsistencyWeekBucket> buckets)
    {
        ConsistencyBuckets.Clear();
        foreach (var b in buckets)
        {
            ConsistencyBuckets.Add(b);
        }
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

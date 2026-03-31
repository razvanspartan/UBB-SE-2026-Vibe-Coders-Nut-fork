using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using VibeCoders.Domain;
using VibeCoders.Models.Analytics;
using VibeCoders.Services;
// using VibeCoders.Models; // TODO: Uncomment when NutritionPlan class is created
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
    // private readonly ClientService _clientService; // TODO: Uncomment when NutritionPlan is implemented
    private CancellationTokenSource? _loadCts;

    public ClientDashboardViewModel(
        IWorkoutAnalyticsStore store,
        IUserSession session,
        IAnalyticsDashboardRefreshBus refreshBus)
        // ClientService clientService) // TODO: Uncomment when NutritionPlan is implemented
    {
        _store = store;
        _session = session;
        _refreshBus = refreshBus;
        // _clientService = clientService; // TODO: Uncomment when NutritionPlan is implemented
        _refreshBus.RefreshRequested += OnRefreshRequested;
    }

    // --- Nutrition Plan Properties (TODO: Implement when NutritionPlan class exists) ---

    // [ObservableProperty]
    // private NutritionPlan? currentNutritionPlan;

    // [ObservableProperty]
    // private bool isLoadingNutrition;

    // ---------------------------------------------


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

    [ObservableProperty]
    private ISeries[] chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] chartXAxes = new[] { new Axis() };

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

    public string PageDisplayText =>
        TotalPages == 0
            ? string.Empty
            : string.Create(CultureInfo.InvariantCulture, $"Page {CurrentPage + 1} of {TotalPages}");

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageDisplayText));
    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(PageDisplayText));

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
            // IsLoadingNutrition = true; // TODO: Uncomment when NutritionPlan is implemented

            await _store.EnsureCreatedAsync(token).ConfigureAwait(true);

            var summaryTask = _store.GetDashboardSummaryAsync(uid, token);
            var bucketsTask = _store.GetConsistencyLastFourWeeksAsync(uid, token);
            CurrentPage = 0;
            var historyTask = _store.GetWorkoutHistoryPageAsync(uid, CurrentPage, PageSize, token);
            // var nutritionTask = Task.Run(() => _clientService.GetActiveNutritionPlan((int)uid), token); // TODO: Uncomment when NutritionPlan is implemented

            await Task.WhenAll(summaryTask, bucketsTask, historyTask).ConfigureAwait(true);
            token.ThrowIfCancellationRequested();

            ApplySummary(summaryTask.Result);
            IsLoadingSummary = false;

            ApplyBuckets(bucketsTask.Result);
            IsLoadingChart = false;

            ApplyHistory(historyTask.Result, uid);
            IsLoadingHistory = false;

            // CurrentNutritionPlan = nutritionTask.Result; // TODO: Uncomment when NutritionPlan is implemented
            // IsLoadingNutrition = false; // TODO: Uncomment when NutritionPlan is implemented
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
                    new SKPoint(0.5f, 1)
                ),
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
                LabelsPaint = new SolidColorPaint(new SKColor(0x8A, 0x8A, 0x8A)), // subtle gray
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

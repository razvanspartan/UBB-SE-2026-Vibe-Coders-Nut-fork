using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Domain;
using VibeCoders.Models.Analytics;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

/// <summary>
/// Represents one row in the workout history list with lazy-loaded
/// set-level detail for the expanded view.
/// </summary>
public sealed partial class WorkoutHistoryItemViewModel : ObservableObject
{
    private readonly IWorkoutAnalyticsStore _store;
    private readonly long _userId;
    private bool _detailLoaded;

    public WorkoutHistoryItemViewModel(
        IWorkoutAnalyticsStore store, long userId, WorkoutHistoryRow row)
    {
        _store = store;
        _userId = userId;
        WorkoutLogId = row.Id;
        Title = row.WorkoutName;
        DateLine = row.LogDate.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        DurationLine = ActiveTimeFormatter.ToHourMinuteSecond(
            TimeSpan.FromSeconds(row.DurationSeconds));
    }

    public int WorkoutLogId { get; }
    public string Title { get; }
    public string DateLine { get; }
    public string DurationLine { get; }

    public ObservableCollection<WorkoutSetRow> Sets { get; } = new();

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isLoadingDetail;

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_detailLoaded)
        {
            _ = LoadDetailAsync();
        }
    }

    [RelayCommand]
    private async Task LoadDetailAsync()
    {
        if (_detailLoaded) return;
        IsLoadingDetail = true;
        try
        {
            var detail = await _store.GetWorkoutSessionDetailAsync(
                _userId, WorkoutLogId).ConfigureAwait(true);
            Sets.Clear();
            if (detail is not null)
            {
                foreach (var s in detail.Sets)
                {
                    Sets.Add(s);
                }
            }

            _detailLoaded = true;
        }
        finally
        {
            IsLoadingDetail = false;
        }
    }
}

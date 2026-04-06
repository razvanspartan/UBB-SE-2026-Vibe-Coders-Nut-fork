using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using VibeCoders.Domain;
using VibeCoders.Models.Analytics;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public sealed partial class WorkoutHistoryItemViewModel : ObservableObject
{
    private readonly IWorkoutAnalyticsStore _store;
    private readonly long _clientId;
    private bool _detailLoaded;

    public WorkoutHistoryItemViewModel(
        IWorkoutAnalyticsStore store, long clientId, WorkoutHistoryRow row)
    {
        _store = store;
        _clientId = clientId;
        WorkoutLogId = row.Id;
        Title = string.IsNullOrWhiteSpace(row.WorkoutName) ? "Workout" : row.WorkoutName;
        DateLine = row.LogDate.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        DurationLine = ActiveTimeFormatter.ToHourMinuteSecond(
            TimeSpan.FromSeconds(row.DurationSeconds));
        TotalCaloriesBurned = row.TotalCaloriesBurned;
        IntensityTag = row.IntensityTag;
    }

    public int WorkoutLogId { get; }
    public string Title { get; }
    public string DateLine { get; }
    public string DurationLine { get; }
    public int TotalCaloriesBurned { get; }
    public string IntensityTag { get; }

    public SolidColorBrush IntensityBrush
    {
        get
        {
            return IntensityTag.ToLower() switch
            {
                "light" => new SolidColorBrush(Colors.Green),
                "moderate" => new SolidColorBrush(Colors.Orange),
                "intense" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
    }

    public ObservableCollection<WorkoutSetRow> Sets { get; } = new();
    public ObservableCollection<ExerciseCalorieInfo> ExerciseCalories { get; } = new();

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
                _clientId, WorkoutLogId).ConfigureAwait(true);
            Sets.Clear();
            ExerciseCalories.Clear();
            if (detail is not null)
            {
                foreach (var s in detail.Sets)
                {
                    Sets.Add(s);
                }
                foreach (var e in detail.ExerciseCalories)
                {
                    ExerciseCalories.Add(e);
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

using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly IWorkoutAnalyticsStore store;
    private readonly long clientId;
    private bool detailLoaded;

    public WorkoutHistoryItemViewModel(
        IWorkoutAnalyticsStore store, long clientId, WorkoutHistoryRow row)
    {
        this.store = store;
        this.clientId = clientId;
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

    public ObservableCollection<ExerciseSetGroupViewModel> ExerciseSetGroups { get; } = new ();
    public ObservableCollection<ExerciseCalorieInfo> ExerciseCalories { get; } = new ();

#pragma warning disable SA1309 // Field names should not begin with underscore
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingDetail { get; set; }
#pragma warning restore SA1309 // Field names should not begin with underscore

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !detailLoaded)
        {
            _ = LoadDetailAsync();
        }
    }

    [RelayCommand]
    private async Task LoadDetailAsync()
    {
        if (detailLoaded)
        {
            return;
        }

        IsLoadingDetail = true;
        try
        {
            var detail = await store.GetWorkoutSessionDetailAsync(
                clientId, WorkoutLogId).ConfigureAwait(true);
            ExerciseSetGroups.Clear();
            ExerciseCalories.Clear();
            if (detail is not null)
            {
                foreach (var group in detail.Sets
                             .GroupBy(s => s.ExerciseName)
                             .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
                {
                    var groupVm = new ExerciseSetGroupViewModel
                    {
                        ExerciseName = group.Key
                    };

                    foreach (var (set, index) in group
                                 .OrderBy(s => s.SetIndex)
                                 .ThenBy(s => s.ExerciseName, StringComparer.CurrentCultureIgnoreCase)
                                 .Select((s, i) => (s, i)))
                    {
                        groupVm.Sets.Add(new SetDetailRowViewModel
                        {
                            SetNumber = index + 1,
                            RepsDisplay = set.RepsDisplay,
                            WeightDisplay = set.WeightDisplay
                        });
                    }

                    ExerciseSetGroups.Add(groupVm);
                }
                foreach (var e in detail.ExerciseCalories)
                {
                                ExerciseCalories.Add(e);
                            }
                        }

                        detailLoaded = true;
                    }
                    finally
                    {
                        IsLoadingDetail = false;
                    }
    }
}

public sealed class ExerciseSetGroupViewModel
{
    public string ExerciseName { get; init; } = string.Empty;
    public ObservableCollection<SetDetailRowViewModel> Sets { get; } = new ();
}

public sealed class SetDetailRowViewModel
{
    private const string EmDash = "—";
    public int SetNumber { get; init; }
    public string RepsDisplay { get; init; } = EmDash;
    public string WeightDisplay { get; init; } = EmDash;
}

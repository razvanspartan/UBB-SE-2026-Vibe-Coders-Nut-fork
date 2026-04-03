using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public sealed partial class WorkoutLogsViewModel : ObservableObject
    {
        private readonly IDataStorage _storage;
        private readonly INavigationService _navigation;

        public WorkoutLogsViewModel(IDataStorage storage, INavigationService navigation)
        {
            _storage = storage;
            _navigation = navigation;
        }

        public ObservableCollection<WorkoutLogItemViewModel> Logs { get; } = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool showEmptyState = true;

        [RelayCommand]
        private void LoadLogs(int clientId)
        {
            try
            {
                IsLoading = true;
                Logs.Clear();

                var logs = _storage.GetWorkoutHistory(clientId);
                foreach (var log in logs)
                {
                    Logs.Add(new WorkoutLogItemViewModel(log));
                }

                ShowEmptyState = Logs.Count == 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void StartWorkout()
        {
            _navigation.NavigateToActiveWorkout();
        }
    }

    public sealed partial class WorkoutLogItemViewModel : ObservableObject
    {
        public int Id { get; }
        public string WorkoutName { get; }
        public DateTime Date { get; }
        public string DateDisplay { get; }

        public string TotalDurationDisplay { get; }

        public List<WorkoutLogExerciseSummary> Exercises { get; }

        [ObservableProperty]
        private bool isExpanded;

        public WorkoutLogItemViewModel(WorkoutLog log)
        {
            Id = log.Id;
            WorkoutName = string.IsNullOrWhiteSpace(log.WorkoutName) ? "Workout" : log.WorkoutName;
            Date = log.Date;

            DateDisplay = log.Date.ToString("yyyy-MM-dd");

            int totalSets = log.Exercises.Sum(e => e.Sets.Count);

            int totalMinutes = totalSets > 0
                ? (totalSets * 1) + ((totalSets - 1) * 3)
                : 0;

            var duration = TimeSpan.FromMinutes(totalMinutes);
            TotalDurationDisplay = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";

            Exercises = log.Exercises
                .Select(e => new WorkoutLogExerciseSummary(e))
                .ToList();
        }
    }

    public sealed class WorkoutLogExerciseSummary
    {
        public string ExerciseName { get; }
        public int NumberOfSets { get; }
        public string RepsDisplay { get; }
        public string WeightDisplay { get; }
        public bool IsSystemAdjusted { get; }
        public string TooltipText { get; }

        public WorkoutLogExerciseSummary(LoggedExercise exercise)
        {
            ExerciseName = exercise.ExerciseName;
            NumberOfSets = exercise.Sets.Count;
            IsSystemAdjusted = exercise.IsSystemAdjusted;

            TooltipText = !string.IsNullOrWhiteSpace(exercise.AdjustmentNote)
                ? exercise.AdjustmentNote
                : $"Performance: {exercise.PerformanceRatio * 100:F0}% of target reps achieved.";

            var reps = exercise.Sets
                .Where(s => s.ActualReps.HasValue)
                .Select(s => s.ActualReps!.Value)
                .ToList();

            var weights = exercise.Sets
                .Where(s => s.ActualWeight.HasValue)
                .Select(s => s.ActualWeight!.Value)
                .ToList();

            RepsDisplay = reps.Count > 0
                ? string.Join(" / ", reps.Select(r => r.ToString()))
                : "—";

            WeightDisplay = weights.Count > 0
                ? string.Join(" / ", weights.Select(w => $"{w} kg"))
                : "—";
        }
    }
}
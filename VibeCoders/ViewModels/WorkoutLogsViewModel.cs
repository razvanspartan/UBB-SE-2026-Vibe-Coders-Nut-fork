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
    /// <summary>
    /// Drives the Workout Logs page — chronological list of training sessions
    /// with collapsed/expanded containers per requirements.
    /// </summary>
    public sealed partial class WorkoutLogsViewModel : ObservableObject
    {
        private readonly IDataStorage _storage;
        private readonly INavigationService _navigation;

        public WorkoutLogsViewModel(IDataStorage storage, INavigationService navigation)
        {
            _storage = storage;
            _navigation = navigation;
        }

        // ── Workout Logs list ────────────────────────────────────────────────

        public ObservableCollection<WorkoutLogItemViewModel> Logs { get; } = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool showEmptyState = true;

        // ── Commands ─────────────────────────────────────────────────────────

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

    /// <summary>
    /// Represents one workout log row in the Workout Logs list.
    /// Collapsed: shows WorkoutName + Total Duration (HH:MM).
    /// Expanded: shows Exercise Name, Sets, Reps, Weight.
    /// Duration formula: (Total Sets * 1 min) + ((Total Sets - 1) * 3 min rest)
    /// </summary>
    public sealed partial class WorkoutLogItemViewModel : ObservableObject
    {
        public int Id { get; }
        public string WorkoutName { get; }
        public DateTime Date { get; }
        public string DateDisplay { get; }

        /// <summary>
        /// Total duration formatted as HH:MM per requirements.
        /// Formula: (TotalSets * 1 min) + ((TotalSets - 1) * 3 min rest)
        /// </summary>
        public string TotalDurationDisplay { get; }

        public List<WorkoutLogExerciseSummary> Exercises { get; }

        [ObservableProperty]
        private bool isExpanded;

        public WorkoutLogItemViewModel(WorkoutLog log)
        {
            Id = log.Id;
            WorkoutName = string.IsNullOrWhiteSpace(log.WorkoutName) ? "Workout" : log.WorkoutName;
            Date = log.Date;

            // YYYY-MM-DD format per requirements (#78)
            DateDisplay = log.Date.ToString("yyyy-MM-dd");

            // Calculate total sets across all exercises
            int totalSets = log.Exercises.Sum(e => e.Sets.Count);

            // Duration formula from requirements:
            // (Total Sets * 1 min) + ((Total Sets - 1) * 3 min rest)
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

    /// <summary>
    /// Summary of one exercise inside an expanded workout log.
    /// Shows: Exercise Name, Number of Sets, Reps per set, Weight.
    /// Also carries progression badge data (#63, #64).
    /// </summary>
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

            // Tooltip text from AdjustmentNote (#64)
            TooltipText = !string.IsNullOrWhiteSpace(exercise.AdjustmentNote)
                ? exercise.AdjustmentNote
                : $"Performance: {exercise.PerformanceRatio * 100:F0}% of target reps achieved.";

            // Aggregate reps and weight across sets for display
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
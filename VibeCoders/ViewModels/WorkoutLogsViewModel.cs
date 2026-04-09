using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public sealed partial class WorkoutLogsViewModel : ObservableObject
    {
        private readonly IDataStorage storage;
        private readonly INavigationService navigation;

        public WorkoutLogsViewModel(IDataStorage storage, INavigationService navigation)
        {
            this.storage = storage;
            this.navigation = navigation;
        }

        public ObservableCollection<WorkoutLogItemViewModel> Logs { get; } = new ();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool showEmptyState = true;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private void LoadLogs(int clientId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                Logs.Clear();
                ShowEmptyState = false;

                var logs = storage.GetWorkoutHistory(clientId);
                foreach (var log in logs)
                {
                    Logs.Add(new WorkoutLogItemViewModel(log));
                }

                ShowEmptyState = Logs.Count == 0;
            }
            catch (Exception ex)
            {
                Logs.Clear();
                ShowEmptyState = true;
                ErrorMessage = $"Failed to load workout logs: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void StartWorkout(int clientId)
        {
            navigation.NavigateToActiveWorkout(clientId);
        }

        [RelayCommand]
        private void ToggleEditMode(WorkoutLogItemViewModel item)
        {
            if (item == null)
            {
                return;
            }

            if (item.IsEditMode)
            {
                item.CancelEditMode();
            }
            else
            {
                item.EnterEditMode();
            }
        }

        [RelayCommand]
        private void SaveEditedLog(WorkoutLogItemViewModel item)
        {
            if (item == null || !item.IsEditMode)
            {
                return;
            }

            try
            {
                ErrorMessage = string.Empty;
                var updated = item.BuildUpdatedWorkoutLog();
                bool ok = storage.UpdateWorkoutLog(updated);
                if (!ok)
                {
                    ErrorMessage = "Failed to save workout changes.";
                    return;
                }

                item.CommitEditMode();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save workout changes: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    public sealed partial class WorkoutLogItemViewModel : ObservableObject
    {
        private readonly WorkoutLog log;
        public int Id { get; }
        public string WorkoutName { get; }
        public DateTime Date { get; }
        public string DateDisplay { get; }
        public string TypeDisplay { get; }

        public string TotalDurationDisplay { get; }

        public ObservableCollection<WorkoutLogExerciseSummary> Exercises { get; } = new ();

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isEditMode;

        public WorkoutLogItemViewModel(WorkoutLog log)
        {
            this.log = log;
            Id = log.Id;
            WorkoutName = string.IsNullOrWhiteSpace(log.WorkoutName) ? "Workout" : log.WorkoutName;
            Date = log.Date;
            TypeDisplay = log.Type switch
            {
                WorkoutType.PREBUILT => "PRE-BUILT",
                WorkoutType.TRAINER_ASSIGNED => "TRAINER ASSIGNED",
                _ => "CUSTOM"
            };

            DateDisplay = log.Date.ToString("yyyy-MM-dd");

            int totalSets = log.Exercises.Sum(e => e.Sets.Count);

            int totalMinutes = totalSets > 0
                ? (totalSets * 1) + ((totalSets - 1) * 3)
                : 0;

            var duration = TimeSpan.FromMinutes(totalMinutes);
            TotalDurationDisplay = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";

            LoadExercisesFromLog(log);
        }

        public void EnterEditMode() => IsEditMode = true;

        public void CancelEditMode()
        {
            LoadExercisesFromLog(log);
            IsEditMode = false;
        }

        public void CommitEditMode()
        {
            log.Exercises = Exercises.Select(e => e.ToLoggedExercise(log.Id)).ToList();
            LoadExercisesFromLog(log);
            IsEditMode = false;
        }

        public WorkoutLog BuildUpdatedWorkoutLog()
        {
            var clone = new WorkoutLog
            {
                Id = log.Id,
                ClientId = log.ClientId,
                WorkoutName = log.WorkoutName,
                Date = log.Date,
                Duration = log.Duration,
                SourceTemplateId = log.SourceTemplateId,
                Type = log.Type,
                TotalCaloriesBurned = log.TotalCaloriesBurned,
                AverageMet = log.AverageMet,
                IntensityTag = log.IntensityTag,
                Rating = log.Rating,
                TrainerNotes = log.TrainerNotes,
                Exercises = Exercises.Select(e => e.ToLoggedExercise(log.Id)).ToList()
            };

            return clone;
        }

        private void LoadExercisesFromLog(WorkoutLog log)
        {
            Exercises.Clear();
            foreach (var exercise in log.Exercises)
            {
                Exercises.Add(new WorkoutLogExerciseSummary(exercise));
            }
        }
    }

    public sealed class WorkoutLogExerciseSummary
    {
        public string ExerciseName { get; }
        public bool IsSystemAdjusted { get; }
        public string TooltipText { get; }
        public ObservableCollection<WorkoutLogSetEditorViewModel> Sets { get; } = new ();

        public int NumberOfSets => Sets.Count;
        public string RepsDisplay => Sets.Count > 0
            ? string.Join(" / ", Sets.Select(s => s.RepsDisplay))
            : "—";
        public string WeightDisplay => Sets.Count > 0
            ? string.Join(" / ", Sets.Select(s => s.WeightDisplay))
            : "—";

        public WorkoutLogExerciseSummary(LoggedExercise exercise)
        {
            ExerciseName = exercise.ExerciseName;
            IsSystemAdjusted = exercise.IsSystemAdjusted;

            TooltipText = !string.IsNullOrWhiteSpace(exercise.AdjustmentNote)
                ? exercise.AdjustmentNote
                : $"Performance: {exercise.PerformanceRatio * 100:F0}% of target reps achieved.";

            int index = 1;
            foreach (var set in exercise.Sets.OrderBy(s => s.SetIndex))
            {
                Sets.Add(new WorkoutLogSetEditorViewModel
                {
                    SetNumber = index++,
                    Reps = set.ActualReps,
                    Weight = set.ActualWeight
                });
            }
        }

        public LoggedExercise ToLoggedExercise(int workoutLogId)
        {
            return new LoggedExercise
            {
                WorkoutLogId = workoutLogId,
                ExerciseName = ExerciseName,
                IsSystemAdjusted = IsSystemAdjusted,
                AdjustmentNote = TooltipText,
                Sets = Sets.Select((s, i) => new LoggedSet
                {
                    WorkoutLogId = workoutLogId,
                    ExerciseName = ExerciseName,
                    SetIndex = i + 1,
                    SetNumber = i + 1,
                    ActualReps = s.Reps,
                    ActualWeight = s.Weight
                }).ToList()
            };
        }
    }

    public sealed partial class WorkoutLogSetEditorViewModel : ObservableObject
    {
        public int SetNumber { get; init; }

        [ObservableProperty]
        private int? reps;

        [ObservableProperty]
        private double? weight;

        public double RepsInput
        {
            get => Reps.HasValue ? Reps.Value : double.NaN;
            set => Reps = double.IsNaN(value) ? null : (int)Math.Round(value);
        }

        public double WeightInput
        {
            get => Weight ?? double.NaN;
            set => Weight = double.IsNaN(value) ? null : value;
        }

        public string RepsDisplay => Reps?.ToString(CultureInfo.InvariantCulture) ?? "—";
        public string WeightDisplay => Weight.HasValue
            ? $"{Weight.Value.ToString("0.##", CultureInfo.InvariantCulture)} kg"
            : "—";

        partial void OnRepsChanged(int? value)
        {
            OnPropertyChanged(nameof(RepsInput));
            OnPropertyChanged(nameof(RepsDisplay));
        }

        partial void OnWeightChanged(double? value)
        {
            OnPropertyChanged(nameof(WeightInput));
            OnPropertyChanged(nameof(WeightDisplay));
        }
    }
}
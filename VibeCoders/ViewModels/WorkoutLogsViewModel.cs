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
        private readonly ClientService clientService;

        public WorkoutLogsViewModel(IDataStorage storage, INavigationService navigation, ClientService clientService)
        {
            this.storage = storage;
            this.navigation = navigation;
            this.clientService = clientService;
        }

        public ObservableCollection<WorkoutLogItemViewModel> Logs { get; } = new ();

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial bool ShowEmptyState { get; set; }

        [ObservableProperty]
        public partial string ErrorMessage { get; set; }

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
                    Logs.Add(new WorkoutLogItemViewModel(log, clientService));
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
        private readonly ClientService clientService;
        public int Id { get; }
        public string WorkoutName { get; }
        public DateTime Date { get; }
        public string DateDisplay { get; }
        public string TypeDisplay { get; }

        public string TotalDurationDisplay { get; }

        public ObservableCollection<WorkoutLogExerciseSummary> Exercises { get; } = new ();

        [ObservableProperty]
        public partial bool IsExpanded { get; set; }

        [ObservableProperty]
        public partial bool IsEditMode { get; set; }

        public WorkoutLogItemViewModel(WorkoutLog log, ClientService clientService)
        {
            this.log = log;
            this.clientService = clientService;
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

            TotalDurationDisplay = this.clientService.BuildEstimatedWorkoutDurationDisplay(log.Exercises);

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
            log.Exercises = BuildUpdatedExerciseCollection();
            LoadExercisesFromLog(log);
            IsEditMode = false;
        }

        public WorkoutLog BuildUpdatedWorkoutLog()
        {
            var updatedExercises = BuildUpdatedExerciseCollection();
            return clientService.BuildUpdatedWorkoutLog(log, updatedExercises);
        }

        private List<LoggedExercise> BuildUpdatedExerciseCollection()
        {
            var updatedExercises = new List<LoggedExercise>();
            for (int exerciseIndex = 0; exerciseIndex < Exercises.Count; exerciseIndex++)
            {
                var exerciseSummaryViewModel = Exercises[exerciseIndex];
                updatedExercises.Add(exerciseSummaryViewModel.ToLoggedExercise(log.Id));
            }

            return updatedExercises;
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
        public partial int? Reps { get; set; }

        [ObservableProperty]
        public partial double? Weight { get; set; }

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
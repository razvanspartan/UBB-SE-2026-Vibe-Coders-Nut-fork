namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using VibeCoders.Models;
    using VibeCoders.Services;

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
                this.IsLoading = true;
                this.ErrorMessage = string.Empty;
                this.Logs.Clear();
                this.ShowEmptyState = false;

                var logs = this.storage.GetWorkoutHistory(clientId);
                foreach (var log in logs)
                {
                    this.Logs.Add(new WorkoutLogItemViewModel(log, this.clientService));
                }

                this.ShowEmptyState = this.Logs.Count == 0;
            }
            catch (Exception ex)
            {
                this.Logs.Clear();
                this.ShowEmptyState = true;
                this.ErrorMessage = $"Failed to load workout logs: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        [RelayCommand]
        private void StartWorkout(int clientId)
        {
            this.navigation.NavigateToActiveWorkout(clientId);
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
                this.ErrorMessage = string.Empty;
                var updated = item.BuildUpdatedWorkoutLog();
                bool ok = this.storage.UpdateWorkoutLog(updated);
                if (!ok)
                {
                    this.ErrorMessage = "Failed to save workout changes.";
                    return;
                }

                item.CommitEditMode();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Failed to save workout changes: {ex.Message}";
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
            this.Id = log.Id;
            this.WorkoutName = string.IsNullOrWhiteSpace(log.WorkoutName) ? "Workout" : log.WorkoutName;
            this.Date = log.Date;
            this.TypeDisplay = log.Type switch
            {
                WorkoutType.PREBUILT => "PRE-BUILT",
                WorkoutType.TRAINER_ASSIGNED => "TRAINER ASSIGNED",
                _ => "CUSTOM"
            };

            this.DateDisplay = log.Date.ToString("yyyy-MM-dd");

            this.TotalDurationDisplay = this.clientService.BuildEstimatedWorkoutDurationDisplay(log.Exercises);

            this.LoadExercisesFromLog(log);
        }

        public void EnterEditMode() => this.IsEditMode = true;

        public void CancelEditMode()
        {
            this.LoadExercisesFromLog(this.log);
            this.IsEditMode = false;
        }

        public void CommitEditMode()
        {
            this.log.Exercises = this.BuildUpdatedExerciseCollection();
            this.LoadExercisesFromLog(this.log);
            this.IsEditMode = false;
        }

        public WorkoutLog BuildUpdatedWorkoutLog()
        {
            var updatedExercises = this.BuildUpdatedExerciseCollection();
            return this.clientService.BuildUpdatedWorkoutLog(this.log, updatedExercises);
        }

        private List<LoggedExercise> BuildUpdatedExerciseCollection()
        {
            var updatedExercises = new List<LoggedExercise>();
            for (int exerciseIndex = 0; exerciseIndex < this.Exercises.Count; exerciseIndex++)
            {
                var exerciseSummaryViewModel = this.Exercises[exerciseIndex];
                updatedExercises.Add(exerciseSummaryViewModel.ToLoggedExercise(this.log.Id));
            }

            return updatedExercises;
        }

        private void LoadExercisesFromLog(WorkoutLog log)
        {
            this.Exercises.Clear();
            foreach (var exercise in log.Exercises)
            {
                this.Exercises.Add(new WorkoutLogExerciseSummary(exercise));
            }
        }
    }

    public sealed class WorkoutLogExerciseSummary
    {
        public string ExerciseName { get; }

        public bool IsSystemAdjusted { get; }

        public string TooltipText { get; }

        public ObservableCollection<WorkoutLogSetEditorViewModel> Sets { get; } = new ();

        public int NumberOfSets => this.Sets.Count;

        public string RepsDisplay => this.Sets.Count > 0
            ? string.Join(" / ", this.Sets.Select(s => s.RepsDisplay))
            : "—";

        public string WeightDisplay => this.Sets.Count > 0
            ? string.Join(" / ", this.Sets.Select(s => s.WeightDisplay))
            : "—";

        public WorkoutLogExerciseSummary(LoggedExercise exercise)
        {
            this.ExerciseName = exercise.ExerciseName;
            this.IsSystemAdjusted = exercise.IsSystemAdjusted;

            this.TooltipText = !string.IsNullOrWhiteSpace(exercise.AdjustmentNote)
                ? exercise.AdjustmentNote
                : $"Performance: {exercise.PerformanceRatio * 100:F0}% of target reps achieved.";

            int index = 1;
            foreach (var set in exercise.Sets.OrderBy(s => s.SetIndex))
            {
                this.Sets.Add(new WorkoutLogSetEditorViewModel
                {
                    SetNumber = index++,
                    Reps = set.ActualReps,
                    Weight = set.ActualWeight,
                });
            }
        }

        public LoggedExercise ToLoggedExercise(int workoutLogId)
        {
            return new LoggedExercise
            {
                WorkoutLogId = workoutLogId,
                ExerciseName = this.ExerciseName,
                IsSystemAdjusted = this.IsSystemAdjusted,
                AdjustmentNote = this.TooltipText,
                Sets = this.Sets.Select((s, i) => new LoggedSet
                {
                    WorkoutLogId = workoutLogId,
                    ExerciseName = this.ExerciseName,
                    SetIndex = i + 1,
                    SetNumber = i + 1,
                    ActualReps = s.Reps,
                    ActualWeight = s.Weight
                }).ToList(),
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
            get => this.Reps.HasValue ? this.Reps.Value : double.NaN;
            set => this.Reps = double.IsNaN(value) ? null : (int)Math.Round(value);
        }

        public double WeightInput
        {
            get => this.Weight ?? double.NaN;
            set => this.Weight = double.IsNaN(value) ? null : value;
        }

        public string RepsDisplay => this.Reps?.ToString(CultureInfo.InvariantCulture) ?? "—";

        public string WeightDisplay => this.Weight.HasValue
            ? $"{this.Weight.Value.ToString("0.##", CultureInfo.InvariantCulture)} kg"
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public sealed partial class ActiveWorkoutViewModel : ObservableObject
    {
        private readonly ClientService clientService;
        private readonly IDataStorage storage;
        private readonly INavigationService navigation;
        private readonly WorkoutUiState workoutUiState;
        private WorkoutLog activeLog;
        private ActiveSetViewModel? currentPendingSet;

        private System.Timers.Timer? restTimer;
        private DispatcherTimer? elapsedTimer;
        private TimeSpan elapsedWorkout;

        public ActiveWorkoutViewModel(
            ClientService clientService,
            IDataStorage storage,
            INavigationService navigation,
            WorkoutUiState workoutUiState)
        {
            this.clientService = clientService;
            this.storage = storage;
            this.navigation = navigation;
            this.workoutUiState = workoutUiState;
            activeLog = new WorkoutLog
            {
                Date = DateTime.Now
            };
        }

        [RelayCommand]
        private void SetRestTime(string? timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
            {
                return;
            }

            if (int.TryParse(timeStr, out int seconds))
            {
                if (seconds < 0)
                {
                    seconds = 0;
                }
                if (seconds > 3600)
                {
                    seconds = 3600; // Cap at 1 hour
                }

                StartRestTimer(seconds);
            }
        }

        public void StartRestTimer(int seconds = 90)
        {
            if (seconds <= 0)
            {
                IsResting = false;
                RestTimeRemaining = 0;
                restTimer?.Stop();
                return;
            }

            var dq = DispatcherQueue.GetForCurrentThread();
            if (dq is null)
            {
                return;
            }

            RestTimeRemaining = seconds;
            IsResting = true;

            restTimer?.Stop();
            restTimer = new System.Timers.Timer(1000);

            restTimer.Elapsed += (_, _) =>
            {
                dq.TryEnqueue(() =>
                {
                    if (RestTimeRemaining > 0)
                    {
                        RestTimeRemaining--;
                    }
                    else
                    {
                        restTimer?.Stop();
                        IsResting = false;
                    }
                });
            };

            restTimer.Start();
        }

        [ObservableProperty]
        private int restTimeRemaining;

        [ObservableProperty]
        private bool isResting;

        [ObservableProperty]
        private string workoutElapsedDisplay = "00:00";

        [ObservableProperty]
        private string workoutSessionTitle = string.Empty;

        [ObservableProperty]
        private string currentExerciseName = string.Empty;

        [ObservableProperty]
        private int? currentTargetReps;

        [ObservableProperty]
        private int currentSetNumber;

        [ObservableProperty]
        private double currentSetRepsInput = double.NaN;

        [ObservableProperty]
        private double currentSetWeightInput = double.NaN;

        partial void OnIsWorkoutStartedChanged(bool value)
        {
            if (value)
            {
                elapsedWorkout = TimeSpan.Zero;
                WorkoutElapsedDisplay = "00:00";
                StartWorkoutElapsedTimer();
            }
            else
            {
                StopWorkoutElapsedTimer();
            }
        }

        private void StartWorkoutElapsedTimer()
        {
            StopWorkoutElapsedTimer();
            elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            elapsedTimer.Tick += (_, _) =>
            {
                elapsedWorkout = elapsedWorkout.Add(TimeSpan.FromSeconds(1));
                WorkoutElapsedDisplay = elapsedWorkout.ToString(@"mm\:ss");
            };
            elapsedTimer.Start();
        }

        private void StopWorkoutElapsedTimer()
        {
            if (elapsedTimer is null)
            {
                return;
            }

            elapsedTimer.Stop();
            elapsedTimer = null;
        }

        private static string? BuildProgressionHeadsUp(WorkoutLog log)
        {
            var lines = log.Exercises
                .Where(e => e.IsSystemAdjusted || !string.IsNullOrWhiteSpace(e.AdjustmentNote))
                .Select(e => string.IsNullOrWhiteSpace(e.AdjustmentNote)
                    ? $"{e.ExerciseName}: targets were adjusted for next time."
                    : $"{e.ExerciseName}: {e.AdjustmentNote}")
                .ToList();
            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        private Dictionary<string, double> GetPreviousBestWeights(int clientId)
        {
            try
            {
                var allLogs = storage.GetWorkoutHistory(clientId);

                return allLogs
                    .SelectMany(log => log.Exercises)
                    .SelectMany(ex => ex.Sets)
                    .GroupBy(s => s.ExerciseName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Max(s => s.ActualWeight ?? 0));
            }
            catch
            {
                return new Dictionary<string, double>();
            }
        }

        [ObservableProperty]
        private ObservableCollection<WorkoutTemplate> availableWorkouts = new ();

        [ObservableProperty]
        private ObservableCollection<WorkoutTemplate> customWorkouts = new ();

        [ObservableProperty]
        private bool hasCustomWorkouts;

        [ObservableProperty]
        private WorkoutTemplate? selectedTemplate;

        [ObservableProperty]
        private bool isLoadingWorkouts;

        [ObservableProperty]
        private string selectedGoal = string.Empty;

        public void LoadCustomWorkouts(int clientId)
        {
            var allWorkouts = storage.GetAvailableWorkouts(clientId);
            CustomWorkouts.Clear();
            foreach (var w in allWorkouts.Where(w =>
                         (w.Type == WorkoutType.CUSTOM || w.Type == WorkoutType.TRAINER_ASSIGNED) &&
                         w.ClientId == clientId))
            {
                CustomWorkouts.Add(w);
            }

            HasCustomWorkouts = CustomWorkouts.Count > 0;
        }

        [RelayCommand]
        private void SelectCustomWorkout(WorkoutTemplate template)
        {
            SelectedTemplate = null;
            SelectedTemplate = template;
        }

        [RelayCommand]
        private void ApplyTargetGoals(int clientId)
        {
            if (string.IsNullOrEmpty(SelectedGoal))
            {
                return;
            }

            var selectedGoalNames = new List<string> { SelectedGoal };

            try
            {
                IsLoadingWorkouts = true;

                var allWorkouts = storage.GetAvailableWorkouts(clientId);
                var selected = allWorkouts
                    .Where(w => selectedGoalNames.Contains(w.Name))
                    .ToList();

                if (selected.Count == 0)
                {
                    return;
                }

                AvailableWorkouts.Clear();
                foreach (var w in selected)
                {
                    AvailableWorkouts.Add(w);
                }

                activeLog = new WorkoutLog
                {
                    WorkoutName = string.Join(" + ", selected.Select(t => t.Name)),
                    SourceTemplateId = selected[0].Id,
                    Type = selected[0].Type,
                    Date = DateTime.Now
                };

                ExerciseRows.Clear();
                foreach (var template in selected)
                {
                    foreach (var exercise in template.GetExercises())
                    {
                        ExerciseRows.Add(new ActiveExerciseViewModel(exercise, SaveSet));
                    }
                }

                UpdateCurrentSetDisplay();
                WorkoutSessionTitle = activeLog.WorkoutName;
                IsWorkoutStarted = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying goals: {ex.Message}");
            }
            finally
            {
                IsLoadingWorkouts = false;
            }
        }

        partial void OnSelectedTemplateChanged(WorkoutTemplate? value)
        {
            if (value == null) return;

            activeLog = new WorkoutLog
            {
                WorkoutName = value.Name,
                SourceTemplateId = value.Id,
                Type = value.Type,
                Date = DateTime.Now
            };

            ExerciseRows.Clear();
            foreach (var exercise in value.GetExercises())
            {
                ExerciseRows.Add(new ActiveExerciseViewModel(exercise, SaveSet));
            }

            UpdateCurrentSetDisplay();
            WorkoutSessionTitle = value.Name;
            IsWorkoutStarted = true;
        }

        [ObservableProperty]
        private ObservableCollection<ActiveExerciseViewModel> exerciseRows = new ();

        [ObservableProperty]
        private bool isWorkoutStarted;

        [ObservableProperty]
        private bool isFinishing;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private void SaveSet(ActiveSetViewModel setViewModel)
        {
            if (setViewModel == null || !IsWorkoutStarted)
            {
                return;
            }

            ErrorMessage = string.Empty;

            var set = new LoggedSet
            {
                ExerciseName = setViewModel.ExerciseName,
                SetIndex = setViewModel.SetIndex,
                ActualReps = setViewModel.ActualReps,
                ActualWeight = setViewModel.ActualWeight,
                TargetReps = setViewModel.TargetReps,
                TargetWeight = null
            };

            bool isSaved = clientService.SaveSet(activeLog, setViewModel.ExerciseName, set);
            if (!isSaved)
            {
                ErrorMessage = "Failed to save set. Please try again.";
                return;
            }

            setViewModel.IsCompleted = true;

            FocusNextSet(setViewModel);
            UpdateCurrentSetDisplay();
        }

        [RelayCommand]
        private void FinishWorkout(int clientId)
        {
            if (!IsWorkoutStarted)
            {
                return;
            }

            try
            {
                IsFinishing = true;
                ErrorMessage = string.Empty;

                activeLog.ClientId = clientId;
                activeLog.Duration = elapsedWorkout;

                bool success = clientService.FinalizeWorkout(activeLog);

                if (success)
                {
                    LastCompletedLog = activeLog;
                    workoutUiState.ProgressionHeadsUp = BuildProgressionHeadsUp(activeLog);
                    IsWorkoutStarted = false;
                    ExerciseRows.Clear();
                    activeLog = new WorkoutLog { Date = DateTime.Now };
                    WorkoutSessionTitle = string.Empty;
                    CurrentExerciseName = string.Empty;
                    CurrentTargetReps = null;
                    CurrentSetNumber = 0;
                    CurrentSetRepsInput = double.NaN;
                    CurrentSetWeightInput = double.NaN;

                    navigation.NavigateToClientDashboard(requestRefresh: true);
                }
                else
                {
                    ErrorMessage = "Failed to save workout. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error finishing workout: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsFinishing = false;
            }
        }

        public WorkoutLog? LastCompletedLog { get; private set; }

        [RelayCommand]
        private void RepeatWorkout(int clientId)
        {
            if (LastCompletedLog == null)
            {
                return;
            }

            var template = storage.GetAvailableWorkouts(clientId)
                .FirstOrDefault(t => t.Id == LastCompletedLog.SourceTemplateId);

            if (template == null)
            {
                return;
            }

            SelectedTemplate = template;
        }

        [ObservableProperty]
        private ObservableCollection<Models.Notification> notifications = new ();

        [RelayCommand]
        private void LoadNotifications(int clientId)
        {
            Notifications.Clear();
            var list = clientService.GetNotifications(clientId);
            foreach (var n in list)
            {
                Notifications.Add(n);
            }
        }

        [RelayCommand]
        private void ConfirmDeload(Models.Notification notification)
        {
            if (notification == null)
            {
                return;
            }

            clientService.ConfirmDeload(notification);
            Notifications.Remove(notification);
        }

        private void FocusNextSet(ActiveSetViewModel completedSet)
        {
            foreach (var exercise in ExerciseRows)
            {
                foreach (var set in exercise.Sets)
                {
                    if (!set.IsCompleted)
                    {
                        set.IsFocused = true;
                        return;
                    }
                }
            }
        }

        private void UpdateCurrentSetDisplay()
        {
            foreach (var exercise in ExerciseRows)
            {
                foreach (var set in exercise.Sets)
                {
                    if (!set.IsCompleted)
                    {
                        currentPendingSet = set;
                        CurrentExerciseName = exercise.ExerciseName;
                        CurrentTargetReps = set.TargetReps;
                        CurrentSetNumber = set.SetIndex;
                        CurrentSetRepsInput = set.ActualRepsValue;
                        CurrentSetWeightInput = set.ActualWeightValue;
                        return;
                    }
                }
            }

            currentPendingSet = null;
            CurrentExerciseName = "Workout complete";
            CurrentTargetReps = null;
            CurrentSetNumber = 0;
            CurrentSetRepsInput = double.NaN;
            CurrentSetWeightInput = double.NaN;
        }

        [RelayCommand]
        private void CompleteCurrentSet()
        {
            if (!IsWorkoutStarted || currentPendingSet is null)
            {
                return;
            }

            currentPendingSet.ActualRepsValue = CurrentSetRepsInput;
            currentPendingSet.ActualWeightValue = CurrentSetWeightInput;
        }
    }

    public sealed partial class ActiveExerciseViewModel : ObservableObject
    {
        public string ExerciseName { get; }
        [ObservableProperty]
        private double? previousBestWeight;
        public MuscleGroup MuscleGroup { get; }
        public ObservableCollection<ActiveSetViewModel> Sets { get; } = new ();

        [ObservableProperty]
        private bool isSystemAdjusted;

        [ObservableProperty]
        private string adjustmentNote = string.Empty;

        public ActiveExerciseViewModel(TemplateExercise template, Action<ActiveSetViewModel> autoSaveSet)
        {
            ExerciseName = template.Name;
            MuscleGroup = template.MuscleGroup;

            for (int i = 0; i < template.TargetSets; i++)
            {
                Sets.Add(new ActiveSetViewModel
                {
                    ExerciseName = template.Name,
                    SetIndex = i + 1,
                    TargetReps = template.TargetReps,
                    TargetWeight = null,
                    IsFocused = i == 0,
                    AutoSaveHandler = autoSaveSet
                });
            }
        }
    }

    public sealed partial class ActiveSetViewModel : ObservableObject
    {
        public string ExerciseName { get; set; } = string.Empty;
        public int SetIndex { get; set; }
        public int? TargetReps { get; set; }
        public double? TargetWeight { get; set; }

        [ObservableProperty]
        private int? actualReps;

        [ObservableProperty]
        private double? actualWeight;

        [ObservableProperty]
        private bool isCompleted;

        [ObservableProperty]
        private bool isFocused;

        public Action<ActiveSetViewModel>? AutoSaveHandler { get; set; }

        public double ActualRepsValue
        {
            get => ActualReps.HasValue ? ActualReps.Value : double.NaN;
            set
            {
                ActualReps = double.IsNaN(value) ? null : (int)Math.Round(value);
            }
        }

        public double ActualWeightValue
        {
            get => ActualWeight ?? double.NaN;
            set
            {
                ActualWeight = double.IsNaN(value) ? null : value;
            }
        }

        partial void OnActualRepsChanged(int? value)
        {
            OnPropertyChanged(nameof(ActualRepsValue));
            TryAutoSave();
        }

        partial void OnActualWeightChanged(double? value)
        {
            OnPropertyChanged(nameof(ActualWeightValue));
            TryAutoSave();
        }

        private void TryAutoSave()
        {
            if (IsCompleted)
            {
                return;
            }

            if (!ActualReps.HasValue || !ActualWeight.HasValue)
            {
                return;
            }

            AutoSaveHandler?.Invoke(this);
        }
    }
}
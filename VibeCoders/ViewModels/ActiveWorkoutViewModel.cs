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
        private readonly ClientService _clientService;
        private readonly IDataStorage _storage;
        private readonly INavigationService _navigation;
        private readonly WorkoutUiState _workoutUiState;
        private WorkoutLog _activeLog;

        private System.Timers.Timer? _restTimer;
        private DispatcherTimer? _elapsedTimer;
        private TimeSpan _elapsedWorkout;

        public ActiveWorkoutViewModel(
            ClientService clientService,
            IDataStorage storage,
            INavigationService navigation,
            WorkoutUiState workoutUiState)
        {
            _clientService = clientService;
            _storage = storage;
            _navigation = navigation;
            _workoutUiState = workoutUiState;
            _activeLog = new WorkoutLog
            {
                Date = DateTime.Now
            };
        }

<<<<<<< HEAD
=======
        public void StartRestTimer(int seconds = 90)
        {
            var dq = DispatcherQueue.GetForCurrentThread();
            if (dq is null) return;

            RestTimeRemaining = seconds;
            IsResting = true;

            _restTimer?.Stop();
            _restTimer = new System.Timers.Timer(1000);

            _restTimer.Elapsed += (_, _) =>
            {
                dq.TryEnqueue(() =>
                {
                    if (RestTimeRemaining > 0)
                        RestTimeRemaining--;
                    else
                    {
                        _restTimer?.Stop();
                        IsResting = false;
                    }
                });
            };

            _restTimer.Start();
        }

        [ObservableProperty]
        private int restTimeRemaining;

        [ObservableProperty]
        private bool isResting;

        [ObservableProperty]
        private string workoutElapsedDisplay = "00:00";

        [ObservableProperty]
        private string workoutSessionTitle = string.Empty;

        partial void OnIsWorkoutStartedChanged(bool value)
        {
            if (value)
            {
                _elapsedWorkout = TimeSpan.Zero;
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
            _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _elapsedTimer.Tick += (_, _) =>
            {
                _elapsedWorkout = _elapsedWorkout.Add(TimeSpan.FromSeconds(1));
                WorkoutElapsedDisplay = _elapsedWorkout.ToString(@"mm\:ss");
            };
            _elapsedTimer.Start();
        }

        private void StopWorkoutElapsedTimer()
        {
            if (_elapsedTimer is null) return;
            _elapsedTimer.Stop();
            _elapsedTimer = null;
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
                var allLogs = _storage.GetWorkoutHistory(clientId);

                return allLogs
                    .SelectMany(log => log.Exercises)
                    .SelectMany(ex => ex.Sets)
                    .GroupBy(s => s.ExerciseName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Max(s => s.ActualWeight ?? 0)
                    );
            }
            catch
            {
                return new Dictionary<string, double>();
            }
        }

>>>>>>> origin/main
        [ObservableProperty]
        private ObservableCollection<WorkoutTemplate> availableWorkouts = new();

        [ObservableProperty]
        private WorkoutTemplate? selectedTemplate;

        [ObservableProperty]
        private bool isLoadingWorkouts;

        [ObservableProperty]
        private bool goalWeightLoss;

        [ObservableProperty]
        private bool goalMuscleGain;

        [ObservableProperty]
        private bool goalRawStrength;

        [ObservableProperty]
        private bool goalMuscularEndurance;

        [RelayCommand]
        private void ApplyTargetGoals(int clientId)
        {
            var selectedGoalNames = new List<string>();

            if (GoalWeightLoss) selectedGoalNames.Add("HIIT Fat Burner");
            if (GoalMuscleGain) selectedGoalNames.Add("Full Body Mass");
            if (GoalRawStrength) selectedGoalNames.Add("Full Body Power");
            if (GoalMuscularEndurance) selectedGoalNames.Add("Endurance Circuit");

            if (selectedGoalNames.Count == 0) return;

            try
            {
                IsLoadingWorkouts = true;

                var allWorkouts = _storage.GetAvailableWorkouts(clientId);
                var selected = allWorkouts
                    .Where(w => selectedGoalNames.Contains(w.Name))
                    .ToList();

                if (selected.Count == 0) return;

                AvailableWorkouts.Clear();
                foreach (var w in selected)
                {
                    AvailableWorkouts.Add(w);
                }

                _activeLog = new WorkoutLog
                {
                    WorkoutName = string.Join(" + ", selected.Select(t => t.Name)),
                    SourceTemplateId = selected[0].Id,
                    Date = DateTime.Now
                };

                ExerciseRows.Clear();
                foreach (var template in selected)
                {
                    foreach (var exercise in template.GetExercises())
                    {
                        ExerciseRows.Add(new ActiveExerciseViewModel(exercise));
                    }
                }

                WorkoutSessionTitle = _activeLog.WorkoutName;
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

            _activeLog = new WorkoutLog
            {
                WorkoutName = value.Name,
                SourceTemplateId = value.Id,
                Date = DateTime.Now
            };

            ExerciseRows.Clear();
            foreach (var exercise in value.GetExercises())
            {
                ExerciseRows.Add(new ActiveExerciseViewModel(exercise));
            }

            WorkoutSessionTitle = value.Name;
            IsWorkoutStarted = true;
        }

        [ObservableProperty]
        private ObservableCollection<ActiveExerciseViewModel> exerciseRows = new();

        [ObservableProperty]
        private bool isWorkoutStarted;

        [ObservableProperty]
        private bool isFinishing;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private void SaveSet(ActiveSetViewModel setViewModel)
        {
            if (setViewModel == null || !IsWorkoutStarted) return;

            var set = new LoggedSet
            {
                ExerciseName = setViewModel.ExerciseName,
                SetIndex = setViewModel.SetIndex,
                ActualReps = setViewModel.ActualReps,
                ActualWeight = setViewModel.ActualWeight,
                TargetReps = setViewModel.TargetReps,
                TargetWeight = setViewModel.TargetWeight
            };

            _clientService.SaveSet(_activeLog, setViewModel.ExerciseName, set);
            setViewModel.IsCompleted = true;

<<<<<<< HEAD
=======
            StartRestTimer();
>>>>>>> origin/main
            FocusNextSet(setViewModel);
        }

        [RelayCommand]
        private void FinishWorkout(int clientId)
        {
            if (!IsWorkoutStarted) return;

            try
            {
                IsFinishing = true;
                ErrorMessage = string.Empty;

                _activeLog.ClientId = clientId;
                _activeLog.Duration = DateTime.Now - _activeLog.Date;

                bool success = _clientService.FinalizeWorkout(_activeLog);

                if (success)
                {
                    LastCompletedLog = _activeLog;
                    _workoutUiState.ProgressionHeadsUp = BuildProgressionHeadsUp(_activeLog);
                    IsWorkoutStarted = false;
                    ExerciseRows.Clear();
                    _activeLog = new WorkoutLog { Date = DateTime.Now };
                    WorkoutSessionTitle = string.Empty;

                    _navigation.NavigateToClientDashboard(requestRefresh: true);
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
            if (LastCompletedLog == null) return;

            var template = _storage.GetAvailableWorkouts(clientId)
                .FirstOrDefault(t => t.Id == LastCompletedLog.SourceTemplateId);

            if (template == null) return;

            SelectedTemplate = template;
        }

        [ObservableProperty]
        private ObservableCollection<Models.Notification> notifications = new();

        [RelayCommand]
        private void LoadNotifications(int clientId)
        {
            Notifications.Clear();
            var list = _clientService.GetNotifications(clientId);
            foreach (var n in list)
            {
                Notifications.Add(n);
            }
        }

        [RelayCommand]
        private void ConfirmDeload(Models.Notification notification)
        {
            if (notification == null) return;
            _clientService.ConfirmDeload(notification);
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
    }

    public sealed partial class ActiveExerciseViewModel : ObservableObject
    {
        public string ExerciseName { get; }
        [ObservableProperty]
        private double? previousBestWeight;
        public MuscleGroup MuscleGroup { get; }
        public ObservableCollection<ActiveSetViewModel> Sets { get; } = new();

        [ObservableProperty]
        private bool isSystemAdjusted;

        [ObservableProperty]
        private string adjustmentNote = string.Empty;

        public ActiveExerciseViewModel(TemplateExercise template)
        {
            ExerciseName = template.Name;
            MuscleGroup = template.MuscleGroup;

            for (int i = 0; i < template.TargetSets; i++)
            {
                Sets.Add(new ActiveSetViewModel
                {
                    ExerciseName = template.Name,
                    SetIndex = i,
                    TargetReps = template.TargetReps,
                    TargetWeight = template.TargetWeight,
                    IsFocused = i == 0
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
        }

        partial void OnActualWeightChanged(double? value)
        {
            OnPropertyChanged(nameof(ActualWeightValue));
        }
    }
}
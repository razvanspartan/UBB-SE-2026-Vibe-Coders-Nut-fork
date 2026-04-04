using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public sealed partial class ActiveWorkoutViewModel : ObservableObject
    {
        private readonly ClientService _clientService;
        private readonly IDataStorage _storage;
        private readonly INavigationService _navigation;
        private WorkoutLog _activeLog;

        public ActiveWorkoutViewModel(
            ClientService clientService,
            IDataStorage storage,
            INavigationService navigation)
        {
            _clientService = clientService;
            _storage = storage;
            _navigation = navigation;
            _activeLog = new WorkoutLog
            {
                Date = DateTime.Now
            };
        }

        private Dictionary<string, double> GetPreviousBestWeights()
        {
            try
            {
                var allLogs = _storage.GetWorkoutLogs();

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
            if (setViewModel == null || SelectedTemplate == null) return;

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
                    IsWorkoutStarted = false;
                    ExerciseRows.Clear();
                    _activeLog = new WorkoutLog { Date = DateTime.Now };

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
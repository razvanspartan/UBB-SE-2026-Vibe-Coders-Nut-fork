using VibeCoders.Services.Interfaces;
namespace VibeCoders.ViewModels
{
    using System.Collections.ObjectModel;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Microsoft.UI.Dispatching;
    using Microsoft.UI.Xaml;
    using VibeCoders.Models;
    using VibeCoders.Services;

    public partial class ActiveWorkoutViewModel : ObservableObject
    {
        private readonly IClientService clientService;
        private readonly INavigationService navigation;
        private readonly WorkoutUiState workoutUiState;
        private WorkoutLog activeLog;
        private ActiveSetViewModel? currentPendingSet;
        private System.Timers.Timer? restTimer;
        private DispatcherTimer? elapsedTimer;
        private TimeSpan elapsedWorkout;
        private const int HourInSeconds = 3600;

        public ActiveWorkoutViewModel(
            IClientService clientService,
            INavigationService navigation,
            WorkoutUiState workoutUiState)
        {
            this.clientService = clientService;
            this.navigation = navigation;
            this.workoutUiState = workoutUiState;
            this.activeLog = new WorkoutLog
            {
                Date = DateTime.Now,
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

                if (seconds > HourInSeconds)
                {
                    seconds = HourInSeconds; // Cap at 1 hour
                }

                this.StartRestTimer(seconds);
            }
        }

        public void StartRestTimer(int seconds = 90)
        {
            if (seconds <= 0)
            {
                this.IsResting = false;
                this.RestTimeRemaining = 0;
                this.restTimer?.Stop();
                return;
            }

            var dq = DispatcherQueue.GetForCurrentThread();
            if (dq is null)
            {
                return;
            }

            this.RestTimeRemaining = seconds;
            this.IsResting = true;

            this.restTimer?.Stop();
            this.restTimer = new System.Timers.Timer(1000);

            this.restTimer.Elapsed += (_, _) =>
            {
                dq.TryEnqueue(() =>
                {
                    if (this.RestTimeRemaining > 0)
                    {
                        this.RestTimeRemaining--;
                    }
                    else
                    {
                        this.restTimer?.Stop();
                        this.IsResting = false;
                    }
                });
            };

            this.restTimer?.Start();
        }

        [ObservableProperty]
        public partial int RestTimeRemaining { get; set; }

        [ObservableProperty]
        public partial bool IsResting { get; set; }

        [ObservableProperty]
        public partial string WorkoutElapsedDisplay { get; set; } = "00:00";

        [ObservableProperty]
        public partial string WorkoutSessionTitle { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string CurrentExerciseName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int? CurrentTargetReps { get; set; }

        [ObservableProperty]
        public partial int CurrentSetNumber { get; set; }

        [ObservableProperty]
        public partial double CurrentSetRepsInput { get; set; } = double.NaN;

        [ObservableProperty]
        public partial double CurrentSetWeightInput { get; set; } = double.NaN;

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
            this.StopWorkoutElapsedTimer();
            this.elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            this.elapsedTimer.Tick += (_, _) =>
            {
                this.elapsedWorkout = this.elapsedWorkout.Add(TimeSpan.FromSeconds(1));
                this.WorkoutElapsedDisplay = this.elapsedWorkout.ToString(@"mm\:ss");
            };
            this.elapsedTimer.Start();
        }

        private void StopWorkoutElapsedTimer()
        {
            if (this.elapsedTimer is null)
            {
                return;
            }

            this.elapsedTimer.Stop();
            this.elapsedTimer = null;
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
            return this.clientService.GetPreviousBestWeights(clientId);
        }

        [ObservableProperty]
        public partial ObservableCollection<WorkoutTemplate> AvailableWorkouts { get; set; } = new ();

        [ObservableProperty]
        public partial ObservableCollection<WorkoutTemplate> CustomWorkouts { get; set; } = new ();

        [ObservableProperty]
        public partial bool HasCustomWorkouts { get; set; }

        [ObservableProperty]
        public partial WorkoutTemplate? SelectedTemplate { get; set; }

        [ObservableProperty]
        public partial bool IsLoadingWorkouts { get; set; }

        [ObservableProperty]
        public partial string SelectedGoal { get; set; } = string.Empty;

        public void LoadCustomWorkouts(int clientId)
        {
            var customAndTrainerAssignedWorkouts = this.clientService.GetCustomAndTrainerAssignedWorkoutsForClient(clientId);
            this.CustomWorkouts.Clear();
            for (int workoutIndex = 0; workoutIndex < customAndTrainerAssignedWorkouts.Count; workoutIndex++)
            {
                this.CustomWorkouts.Add(customAndTrainerAssignedWorkouts[workoutIndex]);
            }

            this.HasCustomWorkouts = this.CustomWorkouts.Count > 0;
        }

        [RelayCommand]
        private void SelectCustomWorkout(WorkoutTemplate template)
        {
            this.SelectedTemplate = null;
            this.SelectedTemplate = template;
        }

        [RelayCommand]
        private void ApplyTargetGoals(int clientId)
        {
            if (string.IsNullOrEmpty(this.SelectedGoal))
            {
                return;
            }

            var selectedGoalNames = new List<string> { this.SelectedGoal };

            try
            {
                this.IsLoadingWorkouts = true;

                var allWorkouts = this.clientService.GetAvailableWorkoutsForClient(clientId);
                var selected = allWorkouts
                    .Where(w => selectedGoalNames.Contains(w.Name))
                    .ToList();

                if (selected.Count == 0)
                {
                    return;
                }

                this.AvailableWorkouts.Clear();
                foreach (var w in selected)
                {
                    this.AvailableWorkouts.Add(w);
                }

                this.activeLog = new WorkoutLog
                {
                    WorkoutName = string.Join(" + ", selected.Select(t => t.Name)),
                    SourceTemplateId = selected[0].Id,
                    Type = selected[0].Type,
                    Date = DateTime.Now,
                };

                this.ExerciseRows.Clear();
                foreach (var template in selected)
                {
                    foreach (var exercise in template.GetExercises())
                    {
                        this.ExerciseRows.Add(new ActiveExerciseViewModel(exercise, this.SaveSet));
                    }
                }

                this.UpdateCurrentSetDisplay();
                this.WorkoutSessionTitle = this.activeLog.WorkoutName;
                this.IsWorkoutStarted = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying goals: {ex.Message}");
            }
            finally
            {
                this.IsLoadingWorkouts = false;
            }
        }

        partial void OnSelectedTemplateChanged(WorkoutTemplate? value)
        {
            if (value == null)
            {
                return;
            }

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
        public partial ObservableCollection<ActiveExerciseViewModel> ExerciseRows { get; set; } = new ();

        [ObservableProperty]
        public partial bool IsWorkoutStarted { get; set; }

        [ObservableProperty]
        public partial bool IsFinishing { get; set; }

        [ObservableProperty]
        public partial string ErrorMessage { get; set; } = string.Empty;

        [RelayCommand]
        private void SaveSet(ActiveSetViewModel setViewModel)
        {
            if (setViewModel == null || !this.IsWorkoutStarted)
            {
                return;
            }

            this.ErrorMessage = string.Empty;

            var set = new LoggedSet
            {
                ExerciseName = setViewModel.ExerciseName,
                SetIndex = setViewModel.SetIndex,
                ActualReps = setViewModel.ActualReps,
                ActualWeight = setViewModel.ActualWeight,
                TargetReps = setViewModel.TargetReps,
                TargetWeight = null,
            };

            bool isSaved = this.clientService.SaveSet(this.activeLog, setViewModel.ExerciseName, set);
            if (!isSaved)
            {
                this.ErrorMessage = "Failed to save set. Please try again.";
                return;
            }

            setViewModel.IsCompleted = true;

            this.FocusNextSet(setViewModel);
            this.UpdateCurrentSetDisplay();
        }

        [RelayCommand]
        private void FinishWorkout(int clientId)
        {
            if (!this.IsWorkoutStarted)
            {
                return;
            }

            try
            {
                this.IsFinishing = true;
                this.ErrorMessage = string.Empty;

                this.activeLog.ClientId = clientId;
                this.activeLog.Duration = this.elapsedWorkout;

                bool success = this.clientService.FinalizeWorkout(this.activeLog);

                if (success)
                {
                    this.LastCompletedLog = this.activeLog;
                    this.workoutUiState.ProgressionHeadsUp = BuildProgressionHeadsUp(this.activeLog);
                    this.IsWorkoutStarted = false;
                    this.ExerciseRows.Clear();
                    this.activeLog = new WorkoutLog { Date = DateTime.Now };
                    this.WorkoutSessionTitle = string.Empty;
                    this.CurrentExerciseName = string.Empty;
                    this.CurrentTargetReps = null;
                    this.CurrentSetNumber = 0;
                    this.CurrentSetRepsInput = double.NaN;
                    this.CurrentSetWeightInput = double.NaN;

                    this.navigation.NavigateToClientDashboard(requestRefresh: true);
                }
                else
                {
                    this.ErrorMessage = "Failed to save workout. Please try again.";
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Error finishing workout: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                this.IsFinishing = false;
            }
        }

        public WorkoutLog? LastCompletedLog { get; private set; }

        [RelayCommand]
        private void RepeatWorkout(int clientId)
        {
            if (this.LastCompletedLog == null)
            {
                return;
            }

            var template = this.clientService.FindWorkoutTemplateById(
                clientId,
                this.LastCompletedLog.SourceTemplateId);

            if (template == null)
            {
                return;
            }

            this.SelectedTemplate = template;
        }

        [ObservableProperty]
        public partial ObservableCollection<Models.Notification> Notifications { get; set; } = new ();

        [RelayCommand]
        private void LoadNotifications(int clientId)
        {
            this.Notifications.Clear();
            var list = this.clientService.GetNotifications(clientId);
            foreach (var n in list)
            {
                this.Notifications.Add(n);
            }
        }

        [RelayCommand]
        private void ConfirmDeload(Models.Notification notification)
        {
            if (notification == null)
            {
                return;
            }

            this.clientService.ConfirmDeload(notification);
            this.Notifications.Remove(notification);
        }

        private void FocusNextSet(ActiveSetViewModel completedSet)
        {
            foreach (var exercise in this.ExerciseRows)
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
            foreach (var exercise in this.ExerciseRows)
            {
                foreach (var set in exercise.Sets)
                {
                    if (!set.IsCompleted)
                    {
                        this.currentPendingSet = set;
                        this.CurrentExerciseName = exercise.ExerciseName;
                        this.CurrentTargetReps = set.TargetReps;
                        this.CurrentSetNumber = set.SetIndex;
                        this.CurrentSetRepsInput = set.ActualRepsValue;
                        this.CurrentSetWeightInput = set.ActualWeightValue;
                        return;
                    }
                }
            }

            this.currentPendingSet = null;
            this.CurrentExerciseName = "Workout complete";
            this.CurrentTargetReps = null;
            this.CurrentSetNumber = 0;
            this.CurrentSetRepsInput = double.NaN;
            this.CurrentSetWeightInput = double.NaN;
        }

        [RelayCommand]
        private void CompleteCurrentSet()
        {
            if (!this.IsWorkoutStarted || this.currentPendingSet is null)
            {
                return;
            }

            this.currentPendingSet.ActualRepsValue = this.CurrentSetRepsInput;
            this.currentPendingSet.ActualWeightValue = this.CurrentSetWeightInput;
        }
    }

    public sealed partial class ActiveExerciseViewModel : ObservableObject
    {
        public string ExerciseName { get; }

        [ObservableProperty]
        public partial double? PreviousBestWeight { get; set; }

        public MuscleGroup MuscleGroup { get; }

        public ObservableCollection<ActiveSetViewModel> Sets { get; } = new ();

        [ObservableProperty]
        public partial bool IsSystemAdjusted { get; set; }

        [ObservableProperty]
        public partial string AdjustmentNote { get; set; } = string.Empty;

        public ActiveExerciseViewModel(TemplateExercise template, Action<ActiveSetViewModel> autoSaveSet)
        {
            this.ExerciseName = template.Name;
            this.MuscleGroup = template.MuscleGroup;

            for (int i = 0; i < template.TargetSets; i++)
            {
                this.Sets.Add(new ActiveSetViewModel
                {
                    ExerciseName = template.Name,
                    SetIndex = i + 1,
                    TargetReps = template.TargetReps,
                    TargetWeight = null,
                    IsFocused = i == 0,
                    AutoSaveHandler = autoSaveSet,
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
        public partial int? ActualReps { get; set; }

        [ObservableProperty]
        public partial double? ActualWeight { get; set; }

        [ObservableProperty]
        public partial bool IsCompleted { get; set; }

        [ObservableProperty]
        public partial bool IsFocused { get; set; }

        public Action<ActiveSetViewModel>? AutoSaveHandler { get; set; }

        public double ActualRepsValue
        {
            get => this.ActualReps.HasValue ? this.ActualReps.Value : double.NaN;
            set
            {
                this.ActualReps = double.IsNaN(value) ? null : (int)Math.Round(value);
            }
        }

        public double ActualWeightValue
        {
            get => this.ActualWeight ?? double.NaN;
            set
            {
                this.ActualWeight = double.IsNaN(value) ? null : value;
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
            if (this.IsCompleted)
            {
                return;
            }

            if (!this.ActualReps.HasValue || !this.ActualWeight.HasValue)
            {
                return;
            }

            this.AutoSaveHandler?.Invoke(this);
        }
    }
}
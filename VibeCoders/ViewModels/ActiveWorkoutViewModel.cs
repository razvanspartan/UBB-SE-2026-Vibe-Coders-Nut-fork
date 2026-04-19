using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public partial class ActiveWorkoutViewModel : ObservableObject
    {
        private readonly ClientService clientService_;
        private readonly IDataStorage storage_;
        private readonly INavigationService navigation_;
        private readonly WorkoutUiState workoutUiState_;
        private WorkoutLog activeLog_;
        private ActiveSetViewModel? currentPendingSet_;
        private System.Timers.Timer? restTimer_;
        private DispatcherTimer? elapsedTimer_;
        private TimeSpan elapsedWorkout_;
        private const int HourInSeconds = 3600;

        public ActiveWorkoutViewModel(
            ClientService clientService,
            IDataStorage storage,
            INavigationService navigation,
            WorkoutUiState workoutUiState)
        {
            clientService_ = clientService;
            storage_ = storage;
            navigation_ = navigation;
            workoutUiState_ = workoutUiState;
            activeLog_ = new WorkoutLog
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
                if (seconds > HourInSeconds)
                {
                    seconds = HourInSeconds; // Cap at 1 hour
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
                restTimer_?.Stop();
                return;
            }

            var dq = DispatcherQueue.GetForCurrentThread();
            if (dq is null)
            {
                return;
            }

            RestTimeRemaining = seconds;
            IsResting = true;

            restTimer_?.Stop();
            restTimer_ = new System.Timers.Timer(1000);

            restTimer_.Elapsed += (_, _) =>
            {
                dq.TryEnqueue(() =>
                {
                    if (RestTimeRemaining > 0)
                    {
                        RestTimeRemaining--;
                    }
                    else
                    {
                        restTimer_?.Stop();
                        IsResting = false;
                    }
                });
            };

            restTimer_?.Start();
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
                elapsedWorkout_ = TimeSpan.Zero;
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
            elapsedTimer_ = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            elapsedTimer_.Tick += (_, _) =>
            {
                elapsedWorkout_ = elapsedWorkout_.Add(TimeSpan.FromSeconds(1));
                WorkoutElapsedDisplay = elapsedWorkout_.ToString(@"mm\:ss");
            };
            elapsedTimer_.Start();
        }

        private void StopWorkoutElapsedTimer()
        {
            if (elapsedTimer_ is null)
            {
                return;
            }
            elapsedTimer_.Stop();
            elapsedTimer_ = null;
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
                var allLogs = storage_.GetWorkoutHistory(clientId);

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
            var allWorkouts = storage_.GetAvailableWorkouts(clientId);
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

                var allWorkouts = storage_.GetAvailableWorkouts(clientId);
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

                activeLog_ = new WorkoutLog
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
                WorkoutSessionTitle = activeLog_.WorkoutName;
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
            if (value == null)
            {
                return;
            }

            activeLog_ = new WorkoutLog
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

            bool isSaved = clientService_.SaveSet(activeLog_, setViewModel.ExerciseName, set);
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

                activeLog_.ClientId = clientId;
                activeLog_.Duration = elapsedWorkout_;

                bool success = clientService_.FinalizeWorkout(activeLog_);

                if (success)
                {
                    LastCompletedLog = activeLog_;
                    workoutUiState_.ProgressionHeadsUp = BuildProgressionHeadsUp(activeLog_);
                    IsWorkoutStarted = false;
                    ExerciseRows.Clear();
                    activeLog_ = new WorkoutLog { Date = DateTime.Now };
                    WorkoutSessionTitle = string.Empty;
                    CurrentExerciseName = string.Empty;
                    CurrentTargetReps = null;
                    CurrentSetNumber = 0;
                    CurrentSetRepsInput = double.NaN;
                    CurrentSetWeightInput = double.NaN;

                    navigation_.NavigateToClientDashboard(requestRefresh: true);
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

            var template = storage_.GetAvailableWorkouts(clientId)
                .FirstOrDefault(t => t.Id == LastCompletedLog.SourceTemplateId);

            if (template == null)
            {
                return;
            }

            SelectedTemplate = template;
        }

        [ObservableProperty]
        public partial ObservableCollection<Models.Notification> Notifications { get; set; } = new ();

        [RelayCommand]
        private void LoadNotifications(int clientId)
        {
            Notifications.Clear();
            var list = clientService_.GetNotifications(clientId);
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
            clientService_.ConfirmDeload(notification);
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
                        currentPendingSet_ = set;
                        CurrentExerciseName = exercise.ExerciseName;
                        CurrentTargetReps = set.TargetReps;
                        CurrentSetNumber = set.SetIndex;
                        CurrentSetRepsInput = set.ActualRepsValue;
                        CurrentSetWeightInput = set.ActualWeightValue;
                        return;
                    }
                }
            }

            currentPendingSet_ = null;
            CurrentExerciseName = "Workout complete";
            CurrentTargetReps = null;
            CurrentSetNumber = 0;
            CurrentSetRepsInput = double.NaN;
            CurrentSetWeightInput = double.NaN;
        }

        [RelayCommand]
        private void CompleteCurrentSet()
        {
            if (!IsWorkoutStarted || currentPendingSet_ is null)
            {
                return;
            }

            currentPendingSet_.ActualRepsValue = CurrentSetRepsInput;
            currentPendingSet_.ActualWeightValue = CurrentSetWeightInput;
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
namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Microsoft.UI.Xaml;
    using VibeCoders.Models;
    using VibeCoders.Services;
    using VibeCoders.Services.Interfaces;

    public sealed partial class TrainerDashboardViewModel : ObservableObject
    {
        private readonly ITrainerService trainerService;
        private readonly INavigationService navigationService;

        private Client? selectedClient;
        private WorkoutLog? selectedWorkoutLog;
        private string newRoutineName = string.Empty;
        private string? selectedNewExercise;
        private double newExerciseSets = 3;
        private double newExerciseReps = 10;
        private double newExerciseWeight;
        private DateTimeOffset planStartDate = DateTimeOffset.Now;
        private DateTimeOffset planEndDate = DateTimeOffset.Now.AddDays(30);
        private string assignmentStatus = string.Empty;

        public TrainerDashboardViewModel(ITrainerService trainerService, INavigationService navigationService)
        {
            this.trainerService = trainerService;
            this.navigationService = navigationService;

            LoadClientsAndWorkouts();
            LoadAvailableExercises();
        }

        public ObservableCollection<Client> AssignedClients { get; } = new ();
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; } = new ();
        public ObservableCollection<ExerciseDisplayRow> CurrentWorkoutDetails { get; } = new ();
        public ObservableCollection<WorkoutTemplate> ClientAssignedWorkouts { get; } = new ();
        public ObservableCollection<TemplateExercise> RoutineBuilderExercises { get; } = new ();
        public ObservableCollection<string> FilteredAvailableExercises { get; } = new ();

        private string builderErrorText = string.Empty;
        public string BuilderErrorText
        {
            get => builderErrorText;
            set
            {
                if (SetProperty(ref builderErrorText, value))
                {
                    OnPropertyChanged(nameof(HasBuilderError));
                }
            }
        }

        public bool HasBuilderError => !string.IsNullOrEmpty(BuilderErrorText);

        private bool isFeedbackFormVisible = true;
        public bool IsFeedbackFormVisible
        {
            get => isFeedbackFormVisible;
            set => SetProperty(ref isFeedbackFormVisible, value);
        }

        private string feedbackErrorText = string.Empty;
        public string FeedbackErrorText
        {
            get => feedbackErrorText;
            set => SetProperty(ref feedbackErrorText, value);
        }

        public int EditingTemplateId { get; set; }

        public Client? SelectedClient
        {
            get => selectedClient;
            set
            {
                if (selectedClient == value)
                {
                    return;
                }

                selectedClient = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAssignPlan));
                LoadLogsForSelectedClient();
                LoadAssignedWorkouts();
            }
        }

        public WorkoutLog? SelectedWorkoutLog
        {
            get => selectedWorkoutLog;
            set
            {
                if (selectedWorkoutLog == value)
                {
                    return;
                }

                selectedWorkoutLog = value;
                OnPropertyChanged();
                OnWorkoutLogSelected();
            }
        }

        public string NewRoutineName
        {
            get => newRoutineName;
            set
            {
                if (newRoutineName == value)
                {
                    return;
                }

                newRoutineName = value;
                OnPropertyChanged();
            }
        }

        public string? SelectedNewExercise
        {
            get => selectedNewExercise;
            set
            {
                if (selectedNewExercise == value)
                {
                    return;
                }

                selectedNewExercise = value;
                OnPropertyChanged();
            }
        }

        public double NewExerciseSets
        {
            get => newExerciseSets;
            set
            {
                if (Math.Abs(newExerciseSets - value) < 0.001)
                {
                    return;
                }

                newExerciseSets = value;
                OnPropertyChanged();
            }
        }

        public double NewExerciseReps
        {
            get => newExerciseReps;
            set
            {
                if (Math.Abs(newExerciseReps - value) < 0.001)
                {
                    return;
                }

                newExerciseReps = value;
                OnPropertyChanged();
            }
        }

        public double NewExerciseWeight
        {
            get => newExerciseWeight;
            set
            {
                if (Math.Abs(newExerciseWeight - value) < 0.001)
                {
                    return;
                }

                newExerciseWeight = value;
                OnPropertyChanged();
            }
        }

        public DateTimeOffset PlanStartDate
        {
            get => planStartDate;
            set
            {
                if (planStartDate == value)
                {
                    return;
                }

                planStartDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAssignPlan));
                OnPropertyChanged(nameof(DateRangeError));
                OnPropertyChanged(nameof(HasDateRangeError));
            }
        }

        public DateTimeOffset PlanEndDate
        {
            get => planEndDate;
            set
            {
                if (planEndDate == value)
                {
                    return;
                }

                planEndDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAssignPlan));
                OnPropertyChanged(nameof(DateRangeError));
                OnPropertyChanged(nameof(HasDateRangeError));
            }
        }

        public string DateRangeError =>
            planEndDate <= planStartDate
                ? "End date must be after start date."
                : string.Empty;

        public bool HasDateRangeError => planEndDate <= planStartDate;

        public bool CanAssignPlan =>
            selectedClient is not null && planEndDate > planStartDate;

        public string AssignmentStatus
        {
            get => assignmentStatus;
            private set
            {
                if (assignmentStatus == value)
                {
                    return;
                }

                assignmentStatus = value;
                OnPropertyChanged();
            }
        }

        public void LoadLogsForSelectedClient()
        {
            SelectedClientLogs.Clear();
            CurrentWorkoutDetails.Clear();

            if (selectedClient is null)
            {
                SelectedWorkoutLog = null;
                return;
            }

            var logs = trainerService.GetClientWorkoutHistory(selectedClient.Id);
            foreach (var log in logs)
            {
                SelectedClientLogs.Add(log);
            }

            SelectedWorkoutLog = SelectedClientLogs.FirstOrDefault();
        }

        public void LoadAssignedWorkouts()
        {
            ClientAssignedWorkouts.Clear();
            if (SelectedClient is null)
            {
                return;
            }

            var allTemplates = trainerService.GetAvailableWorkouts(SelectedClient.Id);
            var trainerAssigned = allTemplates.Where(t => t.Type == WorkoutType.TRAINER_ASSIGNED);
            foreach (var template in trainerAssigned)
            {
                ClientAssignedWorkouts.Add(template);
            }
        }

        public void PrepareForEdit(WorkoutTemplate template)
        {
            EditingTemplateId = template.Id;
            NewRoutineName = template.Name;

            RoutineBuilderExercises.Clear();
            foreach (var ex in template.GetExercises())
            {
                RoutineBuilderExercises.Add(ex);
            }
        }

        public bool DeleteRoutine(WorkoutTemplate template)
        {
            if (template is null)
            {
                return false;
            }

            var success = trainerService.DeleteWorkoutTemplate(template.Id);
            if (success)
            {
                ClientAssignedWorkouts.Remove(template);
            }

            return success;
        }

        public bool BuildAndSaveRoutine()
        {
            BuilderErrorText = string.Empty;
            var clientId = SelectedClient?.Id ?? 0;

            var result = trainerService.AssignNewRoutine(EditingTemplateId, clientId, NewRoutineName, RoutineBuilderExercises);

            if (!result.Success)
            {
                BuilderErrorText = result.ErrorMessage;
                return false;
            }

            LoadAssignedWorkouts();
            return true;
        }

        private void AddExerciseToRoutineCore()
        {
            if (string.IsNullOrWhiteSpace(SelectedNewExercise))
            {
                return;
            }

            var newExercise = new TemplateExercise
            {
                Name = SelectedNewExercise,
                MuscleGroup = MuscleGroup.OTHER,
                TargetSets = (int)newExerciseSets,
                TargetReps = (int)newExerciseReps,
                TargetWeight = newExerciseWeight
            };

            RoutineBuilderExercises.Add(newExercise);
            SelectedNewExercise = null;
        }

        public void AddExerciseToRoutine(object sender, RoutedEventArgs e)
        {
            AddExerciseToRoutineCore();
        }

        public void RemoveExerciseFromRoutine(TemplateExercise exercise)
        {
            if (RoutineBuilderExercises.Contains(exercise))
            {
                RoutineBuilderExercises.Remove(exercise);
            }
        }

        private void SaveCurrentFeedbackCore()
        {
            FeedbackErrorText = string.Empty;

            if (SelectedWorkoutLog is null)
            {
                return;
            }

            if (SelectedWorkoutLog.Rating < 1)
            {
                FeedbackErrorText = "You cannot assign an empty feedback. Please select a star rating.";
                return;
            }

            trainerService.SaveWorkoutFeedback(SelectedWorkoutLog);

            IsFeedbackFormVisible = false;
        }

        public void SaveCurrentFeedback(object sender, RoutedEventArgs e)
        {
            SaveCurrentFeedbackCore();
        }

        private void AssignNutritionPlanCore()
        {
            if (!CanAssignPlan || selectedClient is null)
            {
                return;
            }

            if (!trainerService.CreateAndAssignNutritionPlan(planStartDate.Date, planEndDate.Date, selectedClient.Id))
            {
                AssignmentStatus = "Failed to assign plan.";
                return;
            }

            AssignmentStatus =
                $"Plan assigned to {selectedClient.Username}: " +
                $"{planStartDate.Date:MMM d, yyyy} - {planEndDate.Date:MMM d, yyyy}";
        }

        [RelayCommand]
        private void AssignNutritionPlan()
        {
            AssignNutritionPlanCore();
        }

        private void LoadClientsAndWorkouts()
        {
            AssignedClients.Clear();
            var clients = trainerService.GetAssignedClients(1);
            foreach (var client in clients)
            {
                AssignedClients.Add(client);
            }

            SelectedClient = AssignedClients.FirstOrDefault();
        }

        private void LoadAvailableExercises()
        {
            FilteredAvailableExercises.Clear();
            foreach (var name in trainerService.GetAllExerciseNames())
            {
                FilteredAvailableExercises.Add(name);
            }
        }

        [RelayCommand]
        private void OpenClientProfile()
        {
            if (SelectedClient is null)
            {
                return;
            }

            navigationService.NavigateToClientProfile(SelectedClient.Id);
        }

        [RelayCommand]
        private void OpenWorkoutLogs()
        {
            navigationService.NavigateToWorkoutLogs();
        }

        [RelayCommand]
        private void OpenCalendar()
        {
            navigationService.NavigateToCalendarIntegration();
        }

        private void OnWorkoutLogSelected()
        {
            CurrentWorkoutDetails.Clear();
            IsFeedbackFormVisible = true;
            FeedbackErrorText = string.Empty;

            if (selectedWorkoutLog is null)
            {
                return;
            }

            foreach (var exercise in selectedWorkoutLog.Exercises)
            {
                CurrentWorkoutDetails.Add(new ExerciseDisplayRow
                {
                    Name = exercise.ExerciseName,
                    MuscleGroup = exercise.TargetMuscles.ToString(),
                    Sets = exercise.Sets
                });
            }

            if (selectedWorkoutLog.Rating >= 1)
            {
                IsFeedbackFormVisible = false;
            }
            else
            {
                IsFeedbackFormVisible = true;
            }
        }
    }
}


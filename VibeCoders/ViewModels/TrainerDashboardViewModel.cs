#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1601 // Partial elements should be documented
#pragma warning disable MVVMTK0045

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

    public partial class TrainerDashboardViewModel : ObservableObject
    {
        private const double DefaultSets = 3.0;
        private const double DefaultReps = 10.0;
        private const int DefaultPlanDurationDays = 30;
        private const double FloatingPointTolerance = 0.001;
        private const int MinimumFeedbackRating = 1;
        private const int DefaultTrainerId = 1;
        private const string DateRangeErrorMessage = "End date must be after start date.";
        private const string EmptyFeedbackErrorMessage = "You cannot assign an empty feedback. Please select a star rating.";

        private readonly TrainerService trainerService;
        private readonly INavigationService navigationService;
        private readonly IDataStorage dataStorage;

        private Client? selectedClient;
        private WorkoutLog? selectedWorkoutLog;
        private string newRoutineName = string.Empty;
        private string? selectedNewExercise;
        private double newExerciseSets = TrainerDashboardViewModel.DefaultSets;
        private double newExerciseReps = TrainerDashboardViewModel.DefaultReps;
        private double newExerciseWeight;
        private DateTimeOffset planStartDate = DateTimeOffset.Now;
        private DateTimeOffset planEndDate = DateTimeOffset.Now.AddDays(TrainerDashboardViewModel.DefaultPlanDurationDays);
        private string assignmentStatus = string.Empty;

        [ObservableProperty]
        private string builderErrorText = string.Empty;

        [ObservableProperty]
        private bool isFeedbackFormVisible = true;

        [ObservableProperty]
        private string feedbackErrorText = string.Empty;

        public TrainerDashboardViewModel(
            TrainerService trainerService,
            INavigationService navigationService,
            IDataStorage dataStorage)
        {
            this.trainerService = trainerService;
            this.navigationService = navigationService;
            this.dataStorage = dataStorage;

            this.LoadClientsAndWorkouts();
            this.LoadAvailableExercises();
        }

        public ObservableCollection<Client> AssignedClients { get; } = new ObservableCollection<Client>();

        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; } = new ObservableCollection<WorkoutLog>();

        public ObservableCollection<ExerciseDisplayRow> CurrentWorkoutDetails { get; } = new ObservableCollection<ExerciseDisplayRow>();

        public ObservableCollection<WorkoutTemplate> AssignedWorkouts { get; } = new ObservableCollection<WorkoutTemplate>();

        public ObservableCollection<TemplateExercise> BuilderExercises { get; } = new ObservableCollection<TemplateExercise>();

        public ObservableCollection<string> AvailableExercises { get; } = new ObservableCollection<string>();

        public bool HasBuilderError => !string.IsNullOrEmpty(this.BuilderErrorText);

        public int EditingTemplateId { get; set; }

        public Client? SelectedClient
        {
            get => this.selectedClient;
            set
            {
                if (this.selectedClient == value)
                {
                    return;
                }

                this.selectedClient = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CanAssignPlan));
                this.LoadLogsForSelectedClient();
                this.LoadAssignedWorkouts();
            }
        }

        public WorkoutLog? SelectedWorkoutLog
        {
            get => this.selectedWorkoutLog;
            set
            {
                if (this.selectedWorkoutLog == value)
                {
                    return;
                }

                this.selectedWorkoutLog = value;
                this.OnPropertyChanged();
                this.OnWorkoutLogSelected();
            }
        }

        public string NewRoutineName
        {
            get => this.newRoutineName;
            set
            {
                if (this.newRoutineName == value)
                {
                    return;
                }

                this.newRoutineName = value;
                this.OnPropertyChanged();
            }
        }

        public string? SelectedNewExercise
        {
            get => this.selectedNewExercise;
            set
            {
                if (this.selectedNewExercise == value)
                {
                    return;
                }

                this.selectedNewExercise = value;
                this.OnPropertyChanged();
            }
        }

        public double NewExerciseSets
        {
            get => this.newExerciseSets;
            set
            {
                if (Math.Abs(this.newExerciseSets - value) < TrainerDashboardViewModel.FloatingPointTolerance)
                {
                    return;
                }

                this.newExerciseSets = value;
                this.OnPropertyChanged();
            }
        }

        public double NewExerciseReps
        {
            get => this.newExerciseReps;
            set
            {
                if (Math.Abs(this.newExerciseReps - value) < TrainerDashboardViewModel.FloatingPointTolerance)
                {
                    return;
                }

                this.newExerciseReps = value;
                this.OnPropertyChanged();
            }
        }

        public double NewExerciseWeight
        {
            get => this.newExerciseWeight;
            set
            {
                if (Math.Abs(this.newExerciseWeight - value) < TrainerDashboardViewModel.FloatingPointTolerance)
                {
                    return;
                }

                this.newExerciseWeight = value;
                this.OnPropertyChanged();
            }
        }

        public DateTimeOffset PlanStartDate
        {
            get => this.planStartDate;
            set
            {
                if (this.planStartDate == value)
                {
                    return;
                }

                this.planStartDate = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CanAssignPlan));
                this.OnPropertyChanged(nameof(this.DateRangeError));
                this.OnPropertyChanged(nameof(this.HasDateRangeError));
            }
        }

        public DateTimeOffset PlanEndDate
        {
            get => this.planEndDate;
            set
            {
                if (this.planEndDate == value)
                {
                    return;
                }

                this.planEndDate = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CanAssignPlan));
                this.OnPropertyChanged(nameof(this.DateRangeError));
                this.OnPropertyChanged(nameof(this.HasDateRangeError));
            }
        }

        public string DateRangeError =>
            this.planEndDate <= this.planStartDate
                ? TrainerDashboardViewModel.DateRangeErrorMessage
                : string.Empty;

        public bool HasDateRangeError => this.planEndDate <= this.planStartDate;

        public bool CanAssignPlan =>
            this.selectedClient != null && this.planEndDate > this.planStartDate;

        public string AssignmentStatus
        {
            get => this.assignmentStatus;
            private set
            {
                if (this.assignmentStatus == value)
                {
                    return;
                }

                this.assignmentStatus = value;
                this.OnPropertyChanged();
            }
        }

        partial void OnBuilderErrorTextChanged(string value)
        {
            this.OnPropertyChanged(nameof(this.HasBuilderError));
        }

        public void LoadLogsForSelectedClient()
        {
            this.SelectedClientLogs.Clear();
            this.CurrentWorkoutDetails.Clear();

            if (this.selectedClient == null)
            {
                this.SelectedWorkoutLog = null;
                return;
            }

            var logs = this.trainerService.GetClientWorkoutHistory(this.selectedClient.Id);
            foreach (var workoutLog in logs)
            {
                this.SelectedClientLogs.Add(workoutLog);
            }

            this.SelectedWorkoutLog = this.SelectedClientLogs.FirstOrDefault();
        }

        public void LoadAssignedWorkouts()
        {
            this.AssignedWorkouts.Clear();
            if (this.SelectedClient == null)
            {
                return;
            }

            var allTemplates = this.dataStorage.GetAvailableWorkouts(this.SelectedClient.Id);
            var trainerAssigned = allTemplates.Where(template => template.Type == WorkoutType.TRAINER_ASSIGNED);
            foreach (var trainerTemplate in trainerAssigned)
            {
                this.AssignedWorkouts.Add(trainerTemplate);
            }
        }

        public void PrepareForEdit(WorkoutTemplate template)
        {
            this.EditingTemplateId = template.Id;
            this.NewRoutineName = template.Name;

            this.BuilderExercises.Clear();
            foreach (var templateExercise in template.GetExercises())
            {
                this.BuilderExercises.Add(templateExercise);
            }
        }

        public bool DeleteRoutine(WorkoutTemplate template)
        {
            if (template == null)
            {
                return false;
            }

            var isDeleteSuccessful = this.dataStorage.DeleteWorkoutTemplate(template.Id);
            if (isDeleteSuccessful)
            {
                this.AssignedWorkouts.Remove(template);
            }

            return isDeleteSuccessful;
        }

        public bool SaveRoutine(WorkoutTemplate template)
        {
            return this.trainerService.SaveTrainerWorkout(template);
        }

        public void AddExerciseToRoutine(object sender, RoutedEventArgs routedEventArgs)
        {
            this.AddExerciseToRoutineCore();
        }

        public void RemoveExerciseFromRoutine(TemplateExercise exercise)
        {
            if (this.BuilderExercises.Contains(exercise))
            {
                this.BuilderExercises.Remove(exercise);
            }
        }

        public void SaveCurrentFeedback(object sender, RoutedEventArgs routedEventArgs)
        {
            this.SaveCurrentFeedbackCore();
        }

        [RelayCommand]
        private void AssignNutritionPlan()
        {
            this.AssignNutritionPlanCore();
        }

        [RelayCommand]
        private void OpenClientProfile()
        {
            if (this.SelectedClient == null)
            {
                return;
            }

            this.navigationService.NavigateToClientProfile(this.SelectedClient.Id);
        }

        [RelayCommand]
        private void OpenWorkoutLogs() => this.navigationService.NavigateToWorkoutLogs();

        [RelayCommand]
        private void OpenCalendar() => this.navigationService.NavigateToCalendarIntegration();

        private void AddExerciseToRoutineCore()
        {
            if (string.IsNullOrWhiteSpace(this.SelectedNewExercise))
            {
                return;
            }

            var newExercise = new TemplateExercise
            {
                Name = this.SelectedNewExercise,
                MuscleGroup = MuscleGroup.OTHER,
                TargetSets = (int)this.NewExerciseSets,
                TargetReps = (int)this.NewExerciseReps,
                TargetWeight = this.NewExerciseWeight
            };

            this.BuilderExercises.Add(newExercise);
            this.SelectedNewExercise = null;
        }

        private void SaveCurrentFeedbackCore()
        {
            this.FeedbackErrorText = string.Empty;

            if (this.SelectedWorkoutLog == null)
            {
                return;
            }

            if (this.SelectedWorkoutLog.Rating < TrainerDashboardViewModel.MinimumFeedbackRating)
            {
                this.FeedbackErrorText = TrainerDashboardViewModel.EmptyFeedbackErrorMessage;
                return;
            }

            this.trainerService.SaveWorkoutFeedback(this.SelectedWorkoutLog);

            this.IsFeedbackFormVisible = false;
        }

        private void AssignNutritionPlanCore()
        {
            if (!this.CanAssignPlan || this.selectedClient == null)
            {
                return;
            }

            var nutritionPlan = new NutritionPlan
            {
                StartDate = this.planStartDate.Date,
                EndDate = this.planEndDate.Date,
            };

            if (!this.trainerService.AssignNutritionPlan(nutritionPlan, this.selectedClient.Id))
            {
                return;
            }

            this.AssignmentStatus =
                $"Plan assigned to {this.selectedClient.Username}: " +
                $"{nutritionPlan.StartDate:MMM d, yyyy} - {nutritionPlan.EndDate:MMM d, yyyy}";
        }

        private void LoadClientsAndWorkouts()
        {
            this.AssignedClients.Clear();
            var assignedClientsList = this.trainerService.GetAssignedClients(TrainerDashboardViewModel.DefaultTrainerId);
            foreach (var assignedClient in assignedClientsList)
            {
                this.AssignedClients.Add(assignedClient);
            }

            this.SelectedClient = this.AssignedClients.FirstOrDefault();
        }

        private void LoadAvailableExercises()
        {
            this.AvailableExercises.Clear();
            foreach (var exerciseName in this.dataStorage.GetAllExerciseNames())
            {
                this.AvailableExercises.Add(exerciseName);
            }
        }

        private void OnWorkoutLogSelected()
        {
            this.CurrentWorkoutDetails.Clear();
            this.IsFeedbackFormVisible = true;
            this.FeedbackErrorText = string.Empty;
            if (this.selectedWorkoutLog == null)
            {
                return;
            }

            foreach (var exerciseDisplayItem in this.selectedWorkoutLog.Exercises)
            {
                this.CurrentWorkoutDetails.Add(new ExerciseDisplayRow
                {
                    Name = exerciseDisplayItem.ExerciseName,
                    MuscleGroup = exerciseDisplayItem.TargetMuscles.ToString(),
                    Sets = exerciseDisplayItem.Sets
                });
            }

            if (this.selectedWorkoutLog.Rating >= TrainerDashboardViewModel.MinimumFeedbackRating)
            {
                this.IsFeedbackFormVisible = false;
            }
            else
            {
                this.IsFeedbackFormVisible = true;
            }
        }
    }
}

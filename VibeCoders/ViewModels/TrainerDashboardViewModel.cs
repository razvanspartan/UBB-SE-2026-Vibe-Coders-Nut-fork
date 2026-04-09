#pragma warning disable MVVMTK0045

namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Microsoft.UI.Xaml;
    using VibeCoders.Models;
    using VibeCoders.Services;

    /// <summary>
    /// ViewModel for the trainer dashboard, providing tools for client management, workout feedback, and routine construction.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainerDashboardViewModel"/> class.
        /// </summary>
        /// <param name="trainerService">The trainer service.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="dataStorage">The data storage service.</param>
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

        /// <summary>
        /// Gets the collection of clients assigned to the trainer.
        /// </summary>
        public ObservableCollection<Client> AssignedClients { get; } = new ObservableCollection<Client>();

        /// <summary>
        /// Gets the collection of workout logs for the currently selected client.
        /// </summary>
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; } = new ObservableCollection<WorkoutLog>();

        /// <summary>
        /// Gets the collection of exercise display rows for the currently selected workout log.
        /// </summary>
        public ObservableCollection<ExerciseDisplayRow> CurrentWorkoutDetails { get; } = new ObservableCollection<ExerciseDisplayRow>();

        /// <summary>
        /// Gets the collection of workout templates assigned by the trainer to the selected client.
        /// </summary>
        public ObservableCollection<WorkoutTemplate> AssignedWorkouts { get; } = new ObservableCollection<WorkoutTemplate>();

        /// <summary>
        /// Gets the collection of exercises currently being added to a new routine.
        /// </summary>
        public ObservableCollection<TemplateExercise> BuilderExercises { get; } = new ObservableCollection<TemplateExercise>();

        /// <summary>
        /// Gets the collection of available exercise names from the master catalog.
        /// </summary>
        public ObservableCollection<string> AvailableExercises { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets a value indicating whether there is a routine builder error.
        /// </summary>
        public bool HasBuilderError => !string.IsNullOrEmpty(this.BuilderErrorText);

        /// <summary>
        /// Gets or sets the ID of the template currently being edited.
        /// </summary>
        public int EditingTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the currently selected client.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the currently selected workout log for viewing or feedback.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the name of the new routine being built.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the name of the new exercise to be added to the routine.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the number of sets for the new exercise in the builder.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the number of repetitions for the new exercise in the builder.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the target weight for the new exercise in the builder.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the start date for a new nutrition plan.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the end date for a new nutrition plan.
        /// </summary>
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

        /// <summary>
        /// Gets the error message for invalid date ranges, if any.
        /// </summary>
        public string DateRangeError =>
            this.planEndDate <= this.planStartDate
                ? TrainerDashboardViewModel.DateRangeErrorMessage
                : string.Empty;

        /// <summary>
        /// Gets a value indicating whether the current date range selection is invalid.
        /// </summary>
        public bool HasDateRangeError => this.planEndDate <= this.planStartDate;

        /// <summary>
        /// Gets a value indicating whether a nutrition plan can be assigned.
        /// </summary>
        public bool CanAssignPlan =>
            this.selectedClient != null && this.planEndDate > this.planStartDate;

        /// <summary>
        /// Gets the status message for the last plan assignment.
        /// </summary>
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

        /// <summary>
        /// Loads workout logs for the currently selected client.
        /// </summary>
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

        /// <summary>
        /// Loads workout templates assigned by the trainer for the selected client.
        /// </summary>
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

        /// <summary>
        /// Prepares the routine builder for editing an existing template.
        /// </summary>
        /// <param name="template">The template to edit.</param>
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

        /// <summary>
        /// Deletes a workout routine.
        /// </summary>
        /// <param name="template">The template to delete.</param>
        /// <returns>True if the deletion was successful; otherwise, false.</returns>
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

        /// <summary>
        /// Saves a workout routine template.
        /// </summary>
        /// <param name="template">The template to save.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool SaveRoutine(WorkoutTemplate template)
        {
            return this.trainerService.SaveTrainerWorkout(template);
        }

        /// <summary>
        /// Adds a selected exercise to the routine builder.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="routedEventArgs">The event arguments.</param>
        public void AddExerciseToRoutine(object sender, RoutedEventArgs routedEventArgs)
        {
            this.AddExerciseToRoutineCore();
        }

        /// <summary>
        /// Removes a specific exercise from the routine builder.
        /// </summary>
        /// <param name="exercise">The exercise to remove.</param>
        public void RemoveExerciseFromRoutine(TemplateExercise exercise)
        {
            if (this.BuilderExercises.Contains(exercise))
            {
                this.BuilderExercises.Remove(exercise);
            }
        }

        /// <summary>
        /// Saves the trainer's feedback for the selected workout log.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="routedEventArgs">The event arguments.</param>
        public void SaveCurrentFeedback(object sender, RoutedEventArgs routedEventArgs)
        {
            this.SaveCurrentFeedbackCore();
        }

        partial void OnBuilderErrorTextChanged(string value)
        {
            this.OnPropertyChanged(nameof(this.HasBuilderError));
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

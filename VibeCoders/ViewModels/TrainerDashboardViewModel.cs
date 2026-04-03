using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using VibeCoders.Models;
using VibeCoders.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VibeCoders.ViewModels
{
    public partial class TrainerDashboardViewModel : ObservableObject
    {
        private readonly TrainerService _trainerService;

        public TrainerDashboardViewModel(TrainerService trainerService)
        {
            _trainerService = trainerService;
            LoadClientsAndWorkouts();
            LoadAvailableExercises();
        }


        public ObservableCollection<Client> AssignedClients { get; } = new();
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; } = new();
        public ObservableCollection<ExerciseDisplayRow> CurrentWorkoutDetails { get; } = new();
        public ObservableCollection<WorkoutTemplate> AssignedWorkouts { get; } = new();
        public ObservableCollection<TemplateExercise> BuilderExercises { get; } = new();
        public ObservableCollection<string> AvailableExercises { get; } = new();

        [ObservableProperty]
        private string builderErrorText = string.Empty;


        public bool HasBuilderError => !string.IsNullOrEmpty(BuilderErrorText);

        partial void OnBuilderErrorTextChanged(string value)
        {
            OnPropertyChanged(nameof(HasBuilderError));
        }


        [ObservableProperty]
        private bool isFeedbackFormVisible = true;

        [ObservableProperty]
        private string feedbackErrorText = string.Empty;

        public int EditingTemplateId { get; set; }

        private Client? _selectedClient;
        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (_selectedClient == value) return;
                _selectedClient = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAssignPlan));
                LoadLogsForSelectedClient();
                LoadAssignedWorkouts();
            }
        }

        private WorkoutLog? _selectedWorkoutLog;
        public WorkoutLog? SelectedWorkoutLog
        {
            get => _selectedWorkoutLog;
            set
            {
                if (_selectedWorkoutLog == value) return;
                _selectedWorkoutLog = value;
                OnPropertyChanged();
                OnWorkoutLogSelected();
            }
        }

        private string _newRoutineName = string.Empty;
        public string NewRoutineName
        {
            get => _newRoutineName;
            set
            {
                if (_newRoutineName == value) return;
                _newRoutineName = value;
                OnPropertyChanged();
            }
        }

        private string? _selectedNewExercise;
        public string? SelectedNewExercise
        {
            get => _selectedNewExercise;
            set
            {
                if (_selectedNewExercise == value) return;
                _selectedNewExercise = value;
                OnPropertyChanged();
            }
        }

        private double _newExerciseSets = 3;
        public double NewExerciseSets
        {
            get => _newExerciseSets;
            set
            {
                if (Math.Abs(_newExerciseSets - value) < 0.001) return;
                _newExerciseSets = value;
                OnPropertyChanged();
            }
        }

        private double _newExerciseReps = 10;
        public double NewExerciseReps
        {
            get => _newExerciseReps;
            set
            {
                if (Math.Abs(_newExerciseReps - value) < 0.001) return;
                _newExerciseReps = value;
                OnPropertyChanged();
            }
        }

        private double _newExerciseWeight;
        public double NewExerciseWeight
        {
            get => _newExerciseWeight;
            set
            {
                if (Math.Abs(_newExerciseWeight - value) < 0.001) return;
                _newExerciseWeight = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset _planStartDate = DateTimeOffset.Now;
        public DateTimeOffset PlanStartDate
        {
            get => _planStartDate;
            set
            {
                if (_planStartDate == value) return;
                _planStartDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAssignPlan));
                OnPropertyChanged(nameof(DateRangeError));
                OnPropertyChanged(nameof(HasDateRangeError));
            }
        }

        private DateTimeOffset _planEndDate = DateTimeOffset.Now.AddDays(30);
        public DateTimeOffset PlanEndDate
        {
            get => _planEndDate;
            set
            {
                if (_planEndDate == value) return;
                _planEndDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAssignPlan));
                OnPropertyChanged(nameof(DateRangeError));
                OnPropertyChanged(nameof(HasDateRangeError));
            }
        }

        public string DateRangeError =>
            _planEndDate <= _planStartDate
                ? "End date must be after start date."
                : string.Empty;

        public bool HasDateRangeError => _planEndDate <= _planStartDate;

        public bool CanAssignPlan =>
            _selectedClient != null && _planEndDate > _planStartDate;

        private string _assignmentStatus = string.Empty;
        public string AssignmentStatus
        {
            get => _assignmentStatus;
            private set
            {
                if (_assignmentStatus == value) return;
                _assignmentStatus = value;
                OnPropertyChanged();
            }
        }

        public void LoadLogsForSelectedClient()
        {
            SelectedClientLogs.Clear();
            CurrentWorkoutDetails.Clear();

            if (_selectedClient == null)
            {
                SelectedWorkoutLog = null;
                return;
            }

            var logs = _trainerService.GetClientWorkoutHistory(_selectedClient.Id);
            foreach (var log in logs)
            {
                SelectedClientLogs.Add(log);
            }

            SelectedWorkoutLog = SelectedClientLogs.FirstOrDefault();
        }

        public void LoadAssignedWorkouts()
        {
            AssignedWorkouts.Clear();
            if (SelectedClient == null) return;

            var allTemplates = _trainerService.DataStorage.GetAvailableWorkouts(SelectedClient.Id);
            var trainerAssigned = allTemplates.Where(t => t.Type == WorkoutType.TRAINER_ASSIGNED);
            foreach (var template in trainerAssigned)
            {
                AssignedWorkouts.Add(template);
            }
        }

        public void PrepareForEdit(WorkoutTemplate template)
        {
            EditingTemplateId = template.Id;
            NewRoutineName = template.Name;

            BuilderExercises.Clear();
            foreach (var ex in template.GetExercises())
            {
                BuilderExercises.Add(ex);
            }
        }

        public bool DeleteRoutine(WorkoutTemplate template)
        {
            if (template == null) return false;

            var success = _trainerService.DataStorage.DeleteWorkoutTemplate(template.Id);
            if (success)
            {
                AssignedWorkouts.Remove(template);
            }

            return success;
        }

        public bool SaveRoutine(WorkoutTemplate template)
        {
            return _trainerService.SaveTrainerWorkout(template);
        }

        private void AddExerciseToRoutineCore()
        {
            if (string.IsNullOrWhiteSpace(SelectedNewExercise)) return;

            var newExercise = new TemplateExercise
            {
                Name = SelectedNewExercise,
                MuscleGroup = MuscleGroup.OTHER,
                TargetSets = (int)NewExerciseSets,
                TargetReps = (int)NewExerciseReps,
                TargetWeight = NewExerciseWeight
            };

            BuilderExercises.Add(newExercise);
            SelectedNewExercise = null;
        }

        public void AddExerciseToRoutine(object sender, RoutedEventArgs e)
        {
            AddExerciseToRoutineCore();
        }

        public void RemoveExerciseFromRoutine(TemplateExercise exercise)
        {
            if (BuilderExercises.Contains(exercise))
            {
                BuilderExercises.Remove(exercise);
            }
        }

        private void SaveCurrentFeedbackCore()
        {
            FeedbackErrorText = string.Empty;

            if (SelectedWorkoutLog == null) return;

            if (SelectedWorkoutLog.Rating < 1)
            {
                FeedbackErrorText = "You cannot assign an empty feedback. Please select a star rating.";
                return; 
            }

            _trainerService.SaveWorkoutFeedback(SelectedWorkoutLog);

            IsFeedbackFormVisible = false;
        }

        public void SaveCurrentFeedback(object sender, RoutedEventArgs e)
        {
            SaveCurrentFeedbackCore();
        }

        private void AssignNutritionPlanCore()
        {
            if (!CanAssignPlan || _selectedClient == null) return;

            var plan = new NutritionPlan
            {
                StartDate = _planStartDate.Date,
                EndDate = _planEndDate.Date,
            };

            _trainerService.DataStorage.SaveNutritionPlanForClient(plan, _selectedClient.Id);

            AssignmentStatus =
                $"Plan assigned to {_selectedClient.Username}: " +
                $"{plan.StartDate:MMM d, yyyy} - {plan.EndDate:MMM d, yyyy}";
        }

        public void AssignNutritionPlan(object sender, RoutedEventArgs e)
        {
            AssignNutritionPlanCore();
        }

        private void LoadClientsAndWorkouts()
        {
            AssignedClients.Clear();
            var clients = _trainerService.GetAssignedClients(1);
            foreach (var client in clients)
            {
                AssignedClients.Add(client);
            }

            SelectedClient = AssignedClients.FirstOrDefault();
        }

        private void LoadAvailableExercises()
        {
            AvailableExercises.Clear();
            foreach (var name in _trainerService.DataStorage.GetAllExerciseNames())
            {
                AvailableExercises.Add(name);
            }
        }

        private void OnWorkoutLogSelected()
        {
            CurrentWorkoutDetails.Clear();
            IsFeedbackFormVisible = true;
            FeedbackErrorText = string.Empty;
            if (_selectedWorkoutLog == null) return;

            foreach (var exercise in _selectedWorkoutLog.Exercises)
            {
                CurrentWorkoutDetails.Add(new ExerciseDisplayRow
                {
                    Name = exercise.ExerciseName,
                    MuscleGroup = exercise.TargetMuscles.ToString(),
                    Sets = exercise.Sets
                });
            }

            if (_selectedWorkoutLog.Rating >= 1)
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

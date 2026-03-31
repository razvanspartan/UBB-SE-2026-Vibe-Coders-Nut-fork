using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class TrainerDashboardViewModel
    {
        private readonly TrainerService _trainerService;


        public ObservableCollection<Client> AssignedClients { get; set; } = new ObservableCollection<Client>();
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; set; } = new ObservableCollection<WorkoutLog>();
        public ObservableCollection<ExerciseDisplayRow> CurrentWorkoutDetails { get; set; } = new();

        private Client? _selectedClient;
        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (_selectedClient != value)
                {
                    _selectedClient = value;
                    LoadLogsForSelectedClient();
                }
            }
        }

        

        private WorkoutLog? _selectedWorkoutLog;
        public WorkoutLog? SelectedWorkoutLog
        {
            get => _selectedWorkoutLog;
            set
            {
                if (_selectedWorkoutLog != value)
                {
                    _selectedWorkoutLog = value;
                    OnWorkoutLogSelected();
                }
            }
        }

        private void OnWorkoutLogSelected()
        {
            CurrentWorkoutDetails.Clear();
            if (_selectedWorkoutLog == null) return;

            foreach (var exercise in _selectedWorkoutLog.Exercises)
            {
                CurrentWorkoutDetails.Add(new ExerciseDisplayRow
                {
                    Name = exercise.ExerciseName,
                    MuscleGroup = "Hams", // Ideally pull this from your TemplateExercise data
                    Sets = exercise.Sets
                });
            }
        }


        public TrainerDashboardViewModel(TrainerService trainerService)
        {
            _trainerService = trainerService;
            LoadClientsAndWorkouts();
        }

        private void LoadClientsAndWorkouts()
        {
            AssignedClients.Clear();

            var clients = _trainerService.GetAssignedClients(1);

            foreach (var client in clients)
            {
                AssignedClients.Add(client);
            }
        }

        public void LoadLogsForSelectedClient()
        {
            SelectedClientLogs.Clear();
            CurrentWorkoutDetails.Clear();
            if (_selectedClient != null && _selectedClient.WorkoutLog != null)
            {
                var realLogs = _trainerService.GetClientWorkoutHistory(_selectedClient.Id);
                foreach (var log in realLogs)
                {
                    SelectedClientLogs.Add(log);
                }

                if (SelectedClientLogs.Count > 0)
                {
                    SelectedWorkoutLog = SelectedClientLogs[0];
                }

            }
        }
    }
}
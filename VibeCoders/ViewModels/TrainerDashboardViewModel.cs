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

        private Client? _selectedClient;
        public Client SelectedClient
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
            if (_selectedClient != null && _selectedClient.WorkoutLog != null)
            {
                foreach (var log in _selectedClient.WorkoutLog)
                {
                    SelectedClientLogs.Add(log);
                }
            }
        }
    }
}
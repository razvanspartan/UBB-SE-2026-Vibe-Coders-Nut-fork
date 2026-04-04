using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public partial class ClientProfileViewModel : ObservableObject
    {
        private readonly IClientDataRepository _repository;

        [ObservableProperty]
        private ObservableCollection<LoggedExercise> loggedExercises = new();

        [ObservableProperty]
        private ObservableCollection<Meal> meals = new();

        public string CaloriesBurnedText => $"Calories Burned: 0";

        public ClientProfileViewModel(IClientDataRepository repository)
        {
            _repository = repository;
        }

        public void LoadClientData(int clientId)
        {
            LoggedExercises = new ObservableCollection<LoggedExercise>(
                _repository.GetLoggedExercisesForClient(clientId)
            );

            Meals = new ObservableCollection<Meal>(
                _repository.GetMealsForClient(clientId)
            );
        }
    }
}
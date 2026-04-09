#pragma warning disable MVVMTK0045

namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using VibeCoders.Domain;
    using VibeCoders.Models;
    using VibeCoders.Models.Integration;
    using VibeCoders.Services;

    /// <summary>
    /// ViewModel for the client profile page, managing workout summaries, nutrition sync, and active plans.
    /// </summary>
    public partial class ClientProfileViewModel : ObservableObject
    {
        private const int InvalidClientId = 0;
        private const int DefaultTrainerId = 1;
        private const float DefaultBodyMassIndex = 0.0f;
        private const float MinimumValidMetric = 0.0f;
        private const int EmptyCollectionCount = 0;

        private readonly IDataStorage storage;
        private readonly ClientService clientService;
        private int loadedClientId;

        [ObservableProperty]
        private ObservableCollection<LoggedExercise> loggedExercises = new ObservableCollection<LoggedExercise>();

        [ObservableProperty]
        private ObservableCollection<Meal> meals = new ObservableCollection<Meal>();

        [ObservableProperty]
        private string caloriesSummary = "Calories burned (all logged workouts): 0";

        [ObservableProperty]
        private string latestSessionHint = string.Empty;

        [ObservableProperty]
        private string syncNutritionStatus = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientProfileViewModel"/> class.
        /// </summary>
        /// <param name="storage">The data storage service.</param>
        /// <param name="clientService">The client service.</param>
        public ClientProfileViewModel(IDataStorage storage, ClientService clientService)
        {
            this.storage = storage;
            this.clientService = clientService;
        }

        /// <summary>
        /// Command to synchronize nutrition data for the currently loaded client.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task SyncNutritionAsync()
        {
            if (this.loadedClientId <= ClientProfileViewModel.InvalidClientId)
            {
                return;
            }

            this.SyncNutritionStatus = "Syncing…";

            var workoutHistory = this.storage.GetWorkoutHistory(this.loadedClientId);
            var totalCalories = workoutHistory.Sum(workoutHistoryItem => workoutHistoryItem.TotalCaloriesBurned);
            var lastHistoryItem = workoutHistory.FirstOrDefault();
            var workoutDifficulty = string.IsNullOrWhiteSpace(lastHistoryItem?.IntensityTag) ? "unknown" : lastHistoryItem.IntensityTag;

            float bodyMassIndex = ClientProfileViewModel.DefaultBodyMassIndex;

            List<Client> clientRoster;
            try
            {
                clientRoster = this.storage.GetTrainerClient(ClientProfileViewModel.DefaultTrainerId);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ClientProfileViewModel: Body Mass Index lookup failed; sync continues with UserBmi={ClientProfileViewModel.DefaultBodyMassIndex}. {exception.Message}");
                clientRoster = new List<Client>();
            }

            var profileClient = clientRoster.FirstOrDefault(clientItem => clientItem.Id == this.loadedClientId);
            if (profileClient != null && profileClient.Weight > ClientProfileViewModel.MinimumValidMetric && profileClient.Height > ClientProfileViewModel.MinimumValidMetric)
            {
                bodyMassIndex = (float)BmiCalculator.Calculate(profileClient.Weight, profileClient.Height);
            }

            var nutritionSyncPayload = new NutritionSyncPayload
            {
                TotalCalories = totalCalories,
                WorkoutDifficulty = workoutDifficulty,
                UserBmi = bodyMassIndex
            };

            var isSyncSuccessful = await this.clientService.SyncNutritionAsync(nutritionSyncPayload).ConfigureAwait(true);
            this.SyncNutritionStatus = isSyncSuccessful
                ? "Nutrition sync OK."
                : "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";
        }

        /// <summary>
        /// Loads data for a specific client into the ViewModel.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
        public void LoadClientData(int clientId)
        {
            this.loadedClientId = clientId;
            var workoutHistory = this.storage.GetWorkoutHistory(clientId);
            var totalCaloriesLogged = workoutHistory.Sum(workoutHistoryItem => workoutHistoryItem.TotalCaloriesBurned);
            this.CaloriesSummary = $"Calories burned (all logged workouts): {totalCaloriesLogged}";

            var latestSession = workoutHistory.FirstOrDefault();
            if (latestSession != null && latestSession.Exercises != null && latestSession.Exercises.Count > ClientProfileViewModel.EmptyCollectionCount)
            {
                this.LatestSessionHint = $"Latest session: {latestSession.WorkoutName} — {latestSession.Date:g}";
                this.LoggedExercises = new ObservableCollection<LoggedExercise>(latestSession.Exercises);
            }
            else
            {
                this.LatestSessionHint = "No completed workouts with exercises yet.";
                this.LoggedExercises = new ObservableCollection<LoggedExercise>();
            }

            var nutritionPlan = this.clientService.GetActiveNutritionPlan(clientId);
            if (nutritionPlan != null)
            {
                var mealList = this.storage.GetMealsForPlan(nutritionPlan.PlanId);
                this.Meals = new ObservableCollection<Meal>(mealList);
            }
            else
            {
                this.Meals = new ObservableCollection<Meal>();
            }
        }
    }
}
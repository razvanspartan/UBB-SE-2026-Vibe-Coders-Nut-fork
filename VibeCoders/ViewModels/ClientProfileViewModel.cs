using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public partial class ClientProfileViewModel : ObservableObject
{
    private readonly ClientService _clientService;
    private int _loadedClientId;

    [ObservableProperty]
    private ObservableCollection<LoggedExercise> loggedExercises = new();

    [ObservableProperty]
    private ObservableCollection<Meal> meals = new();

    [ObservableProperty]
    private string caloriesSummary = "Calories burned (all logged workouts): 0";

    [ObservableProperty]
    private string latestSessionHint = string.Empty;

    [ObservableProperty]
    private string syncNutritionStatus = string.Empty;

    public ClientProfileViewModel(ClientService clientService)
    {
        _clientService = clientService;
    }

    [RelayCommand]
    private async Task SyncNutritionAsync()
    {
        if (_loadedClientId <= 0)
        {
            return;
        }

        SyncNutritionStatus = "Syncing…";

        var nutritionSyncPayload = _clientService.BuildNutritionSyncPayload(_loadedClientId);

        var isNutritionSyncSuccessful = await _clientService.SyncNutritionAsync(nutritionSyncPayload).ConfigureAwait(true);
        SyncNutritionStatus = isNutritionSyncSuccessful
            ? "Nutrition sync OK."
            : "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";
    }

    public void LoadClientData(int clientId)
    {
        _loadedClientId = clientId;
        var clientProfileSnapshot = _clientService.BuildClientProfileSnapshot(clientId);

        CaloriesSummary = clientProfileSnapshot.CaloriesSummary;
        LatestSessionHint = clientProfileSnapshot.LatestSessionHint;
        LoggedExercises = new ObservableCollection<LoggedExercise>(clientProfileSnapshot.LoggedExercises);
        Meals = new ObservableCollection<Meal>(clientProfileSnapshot.Meals);
    }
}

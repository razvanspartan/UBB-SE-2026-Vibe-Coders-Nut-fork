using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services.Interfaces;

namespace VibeCoders.ViewModels;

public partial class ClientProfileViewModel : ObservableObject
{
    private readonly IClientService clientService;
    private int loadedClientId;

    [ObservableProperty]
    public partial ObservableCollection<LoggedExercise> LoggedExercises { get; set; } = new ();

    [ObservableProperty]
    public partial ObservableCollection<Meal> Meals { get; set; } = new ();

    [ObservableProperty]
    public partial string CaloriesSummary { get; set; } = "Calories burned (all logged workouts): 0";

    [ObservableProperty]
    public partial string LatestSessionHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SyncNutritionStatus { get; set; } = string.Empty;

    public ClientProfileViewModel(IClientService clientService)
    {
        this.clientService = clientService;
    }

    [RelayCommand]
    private async Task SyncNutritionAsync()
    {
        if (this.loadedClientId <= 0)
        {
            return;
        }

        SyncNutritionStatus = "Syncing…";

        var nutritionSyncPayload = this.clientService.BuildNutritionSyncPayload(this.loadedClientId);

        var isNutritionSyncSuccessful = await this.clientService.SyncNutritionAsync(nutritionSyncPayload).ConfigureAwait(true);
        SyncNutritionStatus = isNutritionSyncSuccessful
            ? "Nutrition sync OK."
            : "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";
    }

    public void LoadClientData(int clientId)
    {
        this.loadedClientId = clientId;
        var clientProfileSnapshot = this.clientService.BuildClientProfileSnapshot(clientId);

        CaloriesSummary = clientProfileSnapshot.CaloriesSummary;
        LatestSessionHint = clientProfileSnapshot.LatestSessionHint;
        LoggedExercises = new ObservableCollection<LoggedExercise>(clientProfileSnapshot.LoggedExercises);
        Meals = new ObservableCollection<Meal>(clientProfileSnapshot.Meals);
    }
}

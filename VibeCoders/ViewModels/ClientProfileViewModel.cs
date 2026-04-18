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

namespace VibeCoders.ViewModels;

public partial class ClientProfileViewModel : ObservableObject
{
    private readonly IDataStorage _storage;
    private readonly IClientService _clientService;
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

    public ClientProfileViewModel(IDataStorage storage, IClientService clientService)
    {
        _storage = storage;
        _clientService = clientService;
    }

    [RelayCommand]
    private async Task SyncNutritionAsync()
    {
        if (_loadedClientId <= 0) return;

        SyncNutritionStatus = "Syncing…";

        var history = _storage.GetWorkoutHistory(_loadedClientId);
        var totalCalories = history.Sum(h => h.TotalCaloriesBurned);
        var last = history.FirstOrDefault();
        var difficulty = string.IsNullOrWhiteSpace(last?.IntensityTag) ? "unknown" : last.IntensityTag;

        float bmi = 0f;
        // UserBmi is optional on the payload: missing roster row or zero weight/height stays at 0 without throwing.
        // Only GetTrainerClient is in try/catch (storage I/O); FirstOrDefault + BMI math run outside so failures there are not swallowed.
        List<Client> roster;
        try
        {
            roster = _storage.GetTrainerClient(1);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ClientProfileViewModel: BMI lookup failed; sync continues with UserBmi=0. {ex.Message}");
            roster = [];
        }

        var profileClient = roster.FirstOrDefault(c => c.Id == _loadedClientId);
        if (profileClient is { Weight: > 0, Height: > 0 })
            bmi = (float)BmiCalculator.Calculate(profileClient.Weight, profileClient.Height);

        var payload = new NutritionSyncPayload
        {
            TotalCalories = totalCalories,
            WorkoutDifficulty = difficulty,
            UserBmi = bmi
        };

        var ok = await _clientService.SyncNutritionAsync(payload).ConfigureAwait(true);
        SyncNutritionStatus = ok
            ? "Nutrition sync OK."
            : "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";
    }

    public void LoadClientData(int clientId)
    {
        _loadedClientId = clientId;
        var history = _storage.GetWorkoutHistory(clientId);
        var totalCal = history.Sum(h => h.TotalCaloriesBurned);
        CaloriesSummary = $"Calories burned (all logged workouts): {totalCal}";

        var latest = history.FirstOrDefault();
        if (latest != null && latest.Exercises is { Count: > 0 })
        {
            LatestSessionHint = $"Latest session: {latest.WorkoutName} — {latest.Date:g}";
            LoggedExercises = new ObservableCollection<LoggedExercise>(latest.Exercises);
        }
        else
        {
            LatestSessionHint = "No completed workouts with exercises yet.";
            LoggedExercises = new ObservableCollection<LoggedExercise>();
        }

        var plan = _clientService.GetActiveNutritionPlan(clientId);
        if (plan != null)
        {
            var mealList = _storage.GetMealsForPlan(plan.PlanId);
            Meals = new ObservableCollection<Meal>(mealList);
        }
        else
        {
            Meals = new ObservableCollection<Meal>();
        }
    }
}

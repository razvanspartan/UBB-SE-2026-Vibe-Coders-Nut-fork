using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public sealed partial class AchievementsViewModel : ObservableObject
{
    private readonly IDataStorage _storage;

    public AchievementsViewModel(IDataStorage storage)
    {
        _storage = storage;
    }

    [ObservableProperty]
    private ObservableCollection<Achievement> achievements = new();

    [ObservableProperty]
    private bool isLoading;

    [RelayCommand]
    private void LoadAchievements(int clientId)
    {
        try
        {
            IsLoading = true;
            Achievements.Clear();
            foreach (var a in _storage.GetAchievements(clientId))
                Achievements.Add(a);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public sealed partial class AchievementsViewModel : ObservableObject
{
    private readonly IDataStorage storage;

    public AchievementsViewModel(IDataStorage storage)
    {
        this.storage = storage;
    }

    [ObservableProperty]
    private ObservableCollection<Achievement> achievements = new ();

    [ObservableProperty]
    private bool isLoading;

    [RelayCommand]
    private void LoadAchievements(int clientId)
    {
        try
        {
            IsLoading = true;
            Achievements.Clear();
            foreach (var a in storage.GetAchievementShowcaseForClient(clientId))
            {
                Achievements.Add(new Achievement
                {
                    AchievementId = a.AchievementId,
                    Name = a.Title,
                    Description = a.Description,
                    Criteria = a.Criteria,
                    IsUnlocked = a.IsUnlocked,
                    Icon = a.IsUnlocked ? "&#xE73E;" : "&#xE72E;"
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}

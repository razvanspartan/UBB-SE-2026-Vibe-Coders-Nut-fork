using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services.Interfaces;

namespace VibeCoders.ViewModels;

public sealed partial class AchievementsViewModel : ObservableObject
{
    private readonly IClientService clientService;

    [ObservableProperty]
    public partial ObservableCollection<Achievement> Achievements { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public AchievementsViewModel(IClientService clientService)
    {
        this.clientService = clientService;
        this.Achievements = new ObservableCollection<Achievement>();
    }

    [RelayCommand]
    private void LoadAchievements(int clientId)
    {
        this.IsLoading = true;
        try
        {
            this.Achievements.Clear();
            foreach (var achievement in this.clientService.GetAchievements(clientId))
            {
                this.Achievements.Add(achievement);
            }
        }
        finally
        {
            this.IsLoading = false;
        }
    }
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Repositories.Interfaces;

namespace VibeCoders.ViewModels;

public sealed partial class AchievementsViewModel : ObservableObject
{
    private readonly IDataStorage storage;
    private readonly IRepositoryAchievements achievementsRepository;

    [ObservableProperty]
    public partial ObservableCollection<Achievement> Achievements { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public AchievementsViewModel(IDataStorage storage, IRepositoryAchievements achievementsRepository)
    {
        this.storage = storage;
        this.achievementsRepository = achievementsRepository;
        this.Achievements = new ObservableCollection<Achievement>();
    }

    [RelayCommand]
    private void LoadAchievements(int clientId)
    {
        this.IsLoading = true;
        try
        {
            this.Achievements.Clear();
            foreach (var a in this.achievementsRepository.GetAchievementShowcaseForClient(clientId))
            {
                this.Achievements.Add(new Achievement
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
            this.IsLoading = false;
        }
    }
}
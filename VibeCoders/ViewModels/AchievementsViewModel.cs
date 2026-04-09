#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1601 // Partial elements should be documented
#pragma warning disable MVVMTK0045

namespace VibeCoders.ViewModels
{
    using System.Collections.ObjectModel;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using VibeCoders.Models;
    using VibeCoders.Services;

    public sealed partial class AchievementsViewModel : ObservableObject
    {
        private const string UnlockedIcon = "&#xE73E;";
        private const string LockedIcon = "&#xE72E;";

        private readonly IDataStorage storage;

        [ObservableProperty]
        private ObservableCollection<Achievement> achievements = new ObservableCollection<Achievement>();

        [ObservableProperty]
        private bool isLoading;

        public AchievementsViewModel(IDataStorage storage)
        {
            this.storage = storage;
        }

        [RelayCommand]
        private void LoadAchievements(int clientId)
        {
            try
            {
                this.IsLoading = true;
                this.Achievements.Clear();

                foreach (var achievementShowcaseItem in this.storage.GetAchievementShowcaseForClient(clientId))
                {
                    this.Achievements.Add(new Achievement
                    {
                        AchievementId = achievementShowcaseItem.AchievementId,
                        Name = achievementShowcaseItem.Title,
                        Description = achievementShowcaseItem.Description,
                        Criteria = achievementShowcaseItem.Criteria,
                        IsUnlocked = achievementShowcaseItem.IsUnlocked,
                        Icon = achievementShowcaseItem.IsUnlocked ? AchievementsViewModel.UnlockedIcon : AchievementsViewModel.LockedIcon
                    });
                }
            }
            finally
            {
                this.IsLoading = false;
            }
        }
    }
}
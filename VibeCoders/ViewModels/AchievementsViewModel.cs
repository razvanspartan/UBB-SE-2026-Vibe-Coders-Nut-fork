#pragma warning disable MVVMTK0045

namespace VibeCoders.ViewModels
{
    using System.Collections.ObjectModel;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using VibeCoders.Models;
    using VibeCoders.Services;

    /// <summary>
    /// ViewModel for the achievements page, responsible for loading and displaying client achievements.
    /// </summary>
    public sealed partial class AchievementsViewModel : ObservableObject
    {
        private const string UnlockedIcon = "&#xE73E;";
        private const string LockedIcon = "&#xE72E;";

        private readonly IDataStorage storage;

        [ObservableProperty]
        private ObservableCollection<Achievement> achievements = new ObservableCollection<Achievement>();

        [ObservableProperty]
        private bool isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="AchievementsViewModel"/> class.
        /// </summary>
        /// <param name="storage">The data storage service.</param>
        public AchievementsViewModel(IDataStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Command to load achievements for a specific client.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
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
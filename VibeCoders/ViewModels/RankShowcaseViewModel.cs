#pragma warning disable MVVMTK0045

namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using VibeCoders.Domain;
    using VibeCoders.Models;
    using VibeCoders.Services;

    /// <summary>
    /// ViewModel for the rank showcase view, managing client rank, levels, and progress.
    /// </summary>
    public sealed partial class RankShowcaseViewModel : ObservableObject
    {
        private const string PlaceholderEmDash = "\u2014";
        private const string DefaultAchievementsDisplay = "0 achievements unlocked";
        private const string DefaultLevelDisplay = "Level \u2014";
        private const string MaxRankInfoDisplay = "Max rank reached \u2014 keep going!";
        private const double MaximumProgressPercent = 100.0;
        private const double PercentMultiplier = 100.0;
        private const int PercentRoundingDecimals = 1;
        private const int InvalidIndex = -1;
        private const int SingularThreshold = 1;
        private const int MinimumBounds = 0;

        private readonly IWorkoutAnalyticsStore analyticsStore;
        private readonly IUserSession userSession;
        private readonly IDataStorage dataStorage;

        [ObservableProperty]
        private int displayLevel;

        [ObservableProperty]
        private string rankTitle = RankShowcaseViewModel.PlaceholderEmDash;

        [ObservableProperty]
        private string unlockedAchievementsDisplay = RankShowcaseViewModel.DefaultAchievementsDisplay;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string levelDisplayLine = RankShowcaseViewModel.DefaultLevelDisplay;

        [ObservableProperty]
        private double progressPercent;

        [ObservableProperty]
        private string nextRankInfo = string.Empty;

        [ObservableProperty]
        private bool hasNextRank;

        /// <summary>
        /// Initializes a new instance of the <see cref="RankShowcaseViewModel"/> class.
        /// </summary>
        /// <param name="analyticsStore">The workout analytics store.</param>
        /// <param name="userSession">The user session service.</param>
        /// <param name="dataStorage">The data storage service.</param>
        public RankShowcaseViewModel(
            IWorkoutAnalyticsStore analyticsStore,
            IUserSession userSession,
            IDataStorage dataStorage)
        {
            this.analyticsStore = analyticsStore;
            this.userSession = userSession;
            this.dataStorage = dataStorage;
        }

        /// <summary>
        /// Gets the collection of achievements for the showcase.
        /// </summary>
        public ObservableCollection<AchievementShowcaseItem> ShowcaseAchievements { get; } = new ObservableCollection<AchievementShowcaseItem>();

        /// <summary>
        /// Loads the rank and achievement data asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            this.Load();
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task RefreshAsync() => this.LoadAsync(CancellationToken.None);

        private void Load()
        {
            this.IsLoading = true;
            try
            {
                var clientId = this.userSession.CurrentClientId;

                var showcase = this.dataStorage.GetAchievementShowcaseForClient((int)clientId);
                int unlockedCount = 0;
                foreach (var showcaseItem in showcase)
                {
                    if (showcaseItem.IsUnlocked)
                    {
                        unlockedCount++;
                    }
                }

                var levelingTiers = LevelingTierEvaluator.DefaultTiers;
                var evaluatorResult = LevelingTierEvaluator.Evaluate(unlockedCount, levelingTiers);

                this.DisplayLevel = evaluatorResult.Level;
                this.RankTitle = evaluatorResult.RankTitle;
                this.LevelDisplayLine = $"Level {evaluatorResult.Level}: {evaluatorResult.RankTitle}";
                this.UnlockedAchievementsDisplay = $"{unlockedCount} achievement{(unlockedCount == RankShowcaseViewModel.SingularThreshold ? string.Empty : "s")} unlocked";

                this.ComputeNextRankProgress(unlockedCount, levelingTiers, evaluatorResult.Level);

                this.ShowcaseAchievements.Clear();
                foreach (var achievementShowcaseRow in showcase)
                {
                    this.ShowcaseAchievements.Add(achievementShowcaseRow);
                }
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        private void ComputeNextRankProgress(
            int unlockedCount,
            IReadOnlyList<LevelTier> tiers,
            int currentLevel)
        {
            int currentIndex = RankShowcaseViewModel.InvalidIndex;
            for (int index = 0; index < tiers.Count; index++)
            {
                if (tiers[index].Level == currentLevel)
                {
                    currentIndex = index;
                    break;
                }
            }

            int nextIndex = currentIndex + 1;
            if (currentIndex < 0 || nextIndex >= tiers.Count)
            {
                this.HasNextRank = false;
                this.ProgressPercent = RankShowcaseViewModel.MaximumProgressPercent;
                this.NextRankInfo = RankShowcaseViewModel.MaxRankInfoDisplay;
                return;
            }

            this.HasNextRank = true;
            var currentTier = tiers[currentIndex];
            var nextTierTier = tiers[nextIndex];

            int currentTierBandStart = currentTier.MinAchievements;
            int nextTierBandEnd = nextTierTier.MinAchievements;
            int earnedAchievements = unlockedCount - currentTierBandStart;
            int neededAchievementsToNextTier = nextTierBandEnd - currentTierBandStart;

            this.ProgressPercent = neededAchievementsToNextTier > 0
                ? Math.Min(RankShowcaseViewModel.MaximumProgressPercent, Math.Round(earnedAchievements * RankShowcaseViewModel.PercentMultiplier / neededAchievementsToNextTier, RankShowcaseViewModel.PercentRoundingDecimals))
                : RankShowcaseViewModel.MaximumProgressPercent;

            int remainingAchievements = Math.Max(RankShowcaseViewModel.MinimumBounds, nextTierBandEnd - unlockedCount);
            this.NextRankInfo = $"Next: Level {nextTierTier.Level}: {nextTierTier.RankTitle} \u2014 {remainingAchievements} more achievement{(remainingAchievements == RankShowcaseViewModel.SingularThreshold ? string.Empty : "s")} to go";
        }
    }
}

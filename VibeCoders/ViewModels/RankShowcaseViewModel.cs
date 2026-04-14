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

namespace VibeCoders.ViewModels;

public sealed partial class RankShowcaseViewModel : ObservableObject
{
    private readonly IWorkoutAnalyticsStore analytics;
    private readonly IUserSession session;
    private readonly IDataStorage data;

    public RankShowcaseViewModel(
        IWorkoutAnalyticsStore analytics,
        IUserSession session,
        IDataStorage data)
    {
        this.analytics = analytics;
        this.session = session;
        this.data = data;
    }

    [ObservableProperty] private int displayLevel;
    [ObservableProperty] private string rankTitle = "\u2014";
    [ObservableProperty] private string unlockedAchievementsDisplay = "0 achievements unlocked";
    [ObservableProperty] private bool isLoading;

    [ObservableProperty] private string levelDisplayLine = "Level \u2014";

    [ObservableProperty] private double progressPercent;

    [ObservableProperty] private string nextRankInfo = string.Empty;

    [ObservableProperty] private bool hasNextRank;

    public ObservableCollection<AchievementShowcaseItem> ShowcaseAchievements { get; } = new ();

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Load();
        return Task.CompletedTask;
    }

    private void Load()
    {
        IsLoading = true;
        try
        {
            var clientId = session.CurrentClientId;

            var showcase = data.GetAchievementShowcaseForClient((int)clientId);
            int unlockedCount = 0;
            foreach (var item in showcase)
            {
                if (item.IsUnlocked)
                {
                    unlockedCount++;
                }
            }

            var tiers = LevelingTierEvaluator.DefaultTiers;
            var result = LevelingTierEvaluator.Evaluate(unlockedCount, tiers);

            DisplayLevel = result.level;
            RankTitle = result.rankTitle;
            LevelDisplayLine = $"Level {result.level}: {result.rankTitle}";
            UnlockedAchievementsDisplay = $"{unlockedCount} achievement{(unlockedCount == 1 ? string.Empty : "s")} unlocked";

            ComputeNextRankProgress(unlockedCount, tiers, result.level);

            ShowcaseAchievements.Clear();
            foreach (var row in showcase)
            {
                ShowcaseAchievements.Add(row);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeNextRankProgress(
        int unlockedCount,
        IReadOnlyList<LevelTier> tiers,
        int currentLevel)
    {
        int currentIndex = -1;
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].level == currentLevel)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex + 1;
        if (currentIndex < 0 || nextIndex >= tiers.Count)
        {
            HasNextRank = false;
            ProgressPercent = 100;
            NextRankInfo = "Max rank reached — keep going!";
            return;
        }

        HasNextRank = true;
        var current = tiers[currentIndex];
        var next = tiers[nextIndex];

        int bandStart = current.minAchievements;
        int bandEnd = next.minAchievements;
        int earned = unlockedCount - bandStart;
        int needed = bandEnd - bandStart;

        ProgressPercent = needed > 0
            ? Math.Min(100, Math.Round(earned * 100.0 / needed, 1))
            : 100;

        int remaining = Math.Max(0, bandEnd - unlockedCount);
        NextRankInfo = $"Next: Level {next.level}: {next.rankTitle} — {remaining} more achievement{(remaining == 1 ? string.Empty : "s")} to go";
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(CancellationToken.None);
}

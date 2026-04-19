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
    private readonly IWorkoutAnalyticsStore _analytics;
    private readonly IUserSession _session;
    private readonly IDataStorage _data;

    public RankShowcaseViewModel(
        IWorkoutAnalyticsStore analytics,
        IUserSession session,
        IDataStorage data)
    {
        _analytics = analytics;
        _session = session;
        _data = data;
    }

    [ObservableProperty]
    private int displayLevel;

    [ObservableProperty]
    private string rankTitle = "\u2014";

    [ObservableProperty]
    private string unlockedAchievementsDisplay = "0 achievements unlocked";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string levelDisplayLine = "Level \u2014";

    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string nextRankInfo = string.Empty;

    [ObservableProperty]
    private bool hasNextRank;

    public ObservableCollection<AchievementShowcaseItem> ShowcaseAchievements { get; } = new();

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        this.Load();
        return Task.CompletedTask;
    }

    private void Load()
    {
        this.IsLoading = true;
        try
        {
            var rankShowcaseSnapshot = evaluationEngine.BuildRankShowcase((int)session.CurrentClientId);

            var showcase = _data.GetAchievementShowcaseForClient((int)clientId);
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

            this.DisplayLevel = result.level;
            this.RankTitle = result.rankTitle;
            this.LevelDisplayLine = $"Level {result.level}: {result.rankTitle}";
            this.UnlockedAchievementsDisplay = $"{unlockedCount} achievement{(unlockedCount == 1 ? "" : "s")} unlocked";

            this.ComputeNextRankProgress(unlockedCount, tiers, result.level);

            this.ShowcaseAchievements.Clear();
            foreach (var row in showcase)
            {
                this.ShowcaseAchievements.Add(row);
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
            this.HasNextRank = false;
            this.ProgressPercent = 100;
            this.NextRankInfo = "Max rank reached — keep going!";
            return;
        }

        this.HasNextRank = true;
        var current = tiers[currentIndex];
        var next = tiers[nextIndex];

        int bandStart = current.minAchievements;
        int bandEnd = next.minAchievements;
        int earned = unlockedCount - bandStart;
        int needed = bandEnd - bandStart;

        this.ProgressPercent = needed > 0
            ? Math.Min(100, Math.Round(earned * 100.0 / needed, 1))
            : 100;

        int remaining = Math.Max(0, bandEnd - unlockedCount);
        this.NextRankInfo = $"Next: Level {next.level}: {next.rankTitle} — {remaining} more achievement{(remaining == 1 ? "" : "s")} to go";
    }

    [RelayCommand]
    private Task RefreshAsync() => this.LoadAsync(CancellationToken.None);
}
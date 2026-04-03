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
    private readonly IWorkoutAnalyticsStore _analytics;
    private readonly IUserSession _session;
    private readonly IDataStorage _data;

    public RankShowcaseViewModel(
        IWorkoutAnalyticsStore analytics,
        IUserSession session,
        IDataStorage data)
    {
        _analytics = analytics;
        _session   = session;
        _data      = data;
    }

    [ObservableProperty] private int    displayLevel;
    [ObservableProperty] private string rankTitle              = "\u2014";
    [ObservableProperty] private string totalActiveTimeDisplay = "0h 00m";
    [ObservableProperty] private bool   isLoading;

    [ObservableProperty] private string levelDisplayLine = "Level \u2014";

    [ObservableProperty] private double progressPercent;

    [ObservableProperty] private string nextRankInfo = string.Empty;

    [ObservableProperty] private bool hasNextRank;

    public ObservableCollection<AchievementShowcaseItem> ShowcaseAchievements { get; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            var userId = _session.CurrentUserId;
            var total  = await _analytics
                .GetTotalActiveTimeAsync(userId, cancellationToken)
                .ConfigureAwait(true);

            var tiers   = LevelingTierEvaluator.DefaultTiers;
            var result  = LevelingTierEvaluator.Evaluate(total, tiers);

            DisplayLevel        = result.Level;
            RankTitle           = result.RankTitle;
            LevelDisplayLine    = $"Level {result.Level}: {result.RankTitle}";
            TotalActiveTimeDisplay = FormatTime(total);

            ComputeNextRankProgress(total, tiers, result.Level);

            ShowcaseAchievements.Clear();
            foreach (var row in _data.GetAchievementShowcaseForClient((int)userId))
                ShowcaseAchievements.Add(row);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeNextRankProgress(
        TimeSpan total,
        IReadOnlyList<LevelTier> tiers,
        int currentLevel)
    {
        int currentIndex = -1;
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].Level == currentLevel)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex + 1;
        if (currentIndex < 0 || nextIndex >= tiers.Count)
        {
            HasNextRank     = false;
            ProgressPercent = 100;
            NextRankInfo    = "Max rank reached — keep going!";
            return;
        }

        HasNextRank = true;
        var current  = tiers[currentIndex];
        var next     = tiers[nextIndex];

        long currentSeconds = (long)Math.Max(0, total.TotalSeconds);
        long bandStart      = current.MinTotalSeconds;
        long bandEnd        = next.MinTotalSeconds;
        long earned         = currentSeconds - bandStart;
        long needed         = bandEnd - bandStart;

        ProgressPercent = needed > 0
            ? Math.Min(100, Math.Round(earned * 100.0 / needed, 1))
            : 100;

        var remaining  = TimeSpan.FromSeconds(Math.Max(0, bandEnd - currentSeconds));
        NextRankInfo   = $"Next: Level {next.Level}: {next.RankTitle} — {FormatTime(remaining)} to go";
    }

    private static string FormatTime(TimeSpan t)
    {
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}h {t.Minutes:D2}m";
        if (t.TotalMinutes >= 1)
            return $"{t.Minutes}m {t.Seconds:D2}s";
        return $"{(int)t.TotalSeconds}s";
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(CancellationToken.None);
}

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public sealed partial class RankShowcaseViewModel : ObservableObject
{
    private readonly EvaluationEngine evaluationEngine;
    private readonly IUserSession session;

    public RankShowcaseViewModel(
        EvaluationEngine evaluationEngine,
        IUserSession session)
    {
        this.evaluationEngine = evaluationEngine;
        this.session = session;
    }

    [ObservableProperty] private int    displayLevel;
    [ObservableProperty] private string rankTitle              = "\u2014";
    [ObservableProperty] private string unlockedAchievementsDisplay = "0 achievements unlocked";
    [ObservableProperty] private bool   isLoading;

    [ObservableProperty] private string levelDisplayLine = "Level \u2014";

    [ObservableProperty] private double progressPercent;

    [ObservableProperty] private string nextRankInfo = string.Empty;

    [ObservableProperty] private bool hasNextRank;

    public ObservableCollection<AchievementShowcaseItem> ShowcaseAchievements { get; } = new();

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
            var rankShowcaseSnapshot = evaluationEngine.BuildRankShowcase((int)session.CurrentClientId);

            DisplayLevel = rankShowcaseSnapshot.DisplayLevel;
            RankTitle = rankShowcaseSnapshot.RankTitle;
            LevelDisplayLine = rankShowcaseSnapshot.LevelDisplayLine;
            UnlockedAchievementsDisplay = rankShowcaseSnapshot.UnlockedAchievementsDisplay;
            HasNextRank = rankShowcaseSnapshot.HasNextRank;
            ProgressPercent = rankShowcaseSnapshot.ProgressPercent;
            NextRankInfo = rankShowcaseSnapshot.NextRankInfo;

            ShowcaseAchievements.Clear();
            foreach (var achievementShowcaseItem in rankShowcaseSnapshot.ShowcaseAchievements)
            {
                ShowcaseAchievements.Add(achievementShowcaseItem);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync()
    {
        return LoadAsync(CancellationToken.None);
    }
}

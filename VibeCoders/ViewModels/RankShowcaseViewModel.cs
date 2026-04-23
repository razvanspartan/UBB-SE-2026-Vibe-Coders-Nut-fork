using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Services.Interfaces;

namespace VibeCoders.ViewModels;

/// <summary>
/// View model for the rank showcase view, responsible for displaying user progression and achievements.
/// </summary>
public sealed partial class RankShowcaseViewModel : ObservableObject
{
    private readonly IEvaluationEngine evaluationEngine;
    private readonly IUserSession session;

    public RankShowcaseViewModel(
        IEvaluationEngine evaluationEngine,
        IUserSession session)
    {
        this.evaluationEngine = evaluationEngine;
        this.session = session;
    }

    [ObservableProperty]
    public partial int DisplayLevel { get; set; }

    [ObservableProperty]
    public partial string RankTitle { get; set; } = "\u2014";

    [ObservableProperty]
    public partial string UnlockedAchievementsDisplay { get; set; } = "0 achievements unlocked";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string LevelDisplayLine { get; set; } = "Level \u2014";

    [ObservableProperty]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    public partial string NextRankInfo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasNextRank { get; set; }
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

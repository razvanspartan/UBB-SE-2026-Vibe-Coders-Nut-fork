using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.Tests.Mocks.DataFactories;

public static class RankShowcaseDataFactory
{
    private const int ExistingAchievementId = 1;
    private const string ExistingAchievementTitle = "Old achievement";
    private const int ConsistencyAchievementId = 2;
    private const string ConsistencyAchievementTitle = "Consistency";
    private const string ConsistencyAchievementDescription = "Train three days in a row.";
    private const string ConsistencyAchievementCriteria = "3-day streak";
    private const int MomentumAchievementId = 3;
    private const string MomentumAchievementTitle = "Momentum";
    private const string MomentumAchievementDescription = "Complete four workouts in a week.";
    private const string MomentumAchievementCriteria = "4 workouts in 7 days";
    private const int DisplayLevel = 5;
    private const string RankTitle = "Gym Enthusiast";
    private const string UnlockedAchievementsDisplay = "5 achievements unlocked";
    private const string LevelDisplayLine = "Level 5";
    private const double ProgressPercent = 62.5;
    private const string NextRankInfo = "Next: Level 6 Athlete — 2 more achievements to go";

    public static AchievementShowcaseItem CreateExistingAchievementShowcaseItem()
    {
        return new AchievementShowcaseItem
        {
            AchievementId = ExistingAchievementId,
            Title = ExistingAchievementTitle,
        };
    }

    public static AchievementShowcaseItem CreateUnlockedConsistencyAchievementShowcaseItem()
    {
        return new AchievementShowcaseItem
        {
            AchievementId = ConsistencyAchievementId,
            Title = ConsistencyAchievementTitle,
            Description = ConsistencyAchievementDescription,
            Criteria = ConsistencyAchievementCriteria,
            IsUnlocked = true,
        };
    }

    public static AchievementShowcaseItem CreateLockedMomentumAchievementShowcaseItem()
    {
        return new AchievementShowcaseItem
        {
            AchievementId = MomentumAchievementId,
            Title = MomentumAchievementTitle,
            Description = MomentumAchievementDescription,
            Criteria = MomentumAchievementCriteria,
            IsUnlocked = false,
        };
    }

    public static RankShowcaseSnapshot CreateRankShowcaseSnapshot(params AchievementShowcaseItem[] showcaseAchievements)
    {
        return new RankShowcaseSnapshot
        {
            DisplayLevel = DisplayLevel,
            RankTitle = RankTitle,
            UnlockedAchievementsDisplay = UnlockedAchievementsDisplay,
            LevelDisplayLine = LevelDisplayLine,
            ProgressPercent = ProgressPercent,
            NextRankInfo = NextRankInfo,
            HasNextRank = true,
            ShowcaseAchievements = showcaseAchievements,
        };
    }
}
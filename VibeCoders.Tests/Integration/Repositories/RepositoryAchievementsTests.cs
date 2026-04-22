using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryAchievementsTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryAchievements repository;

    public RepositoryAchievementsTests()
    {
        this.connectionString = "Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared";
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        CreateSchema(this.connection);

        this.repository = new RepositoryAchievements(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var cmd = new SqliteCommand(
            @"
            CREATE TABLE IF NOT EXISTS CLIENT (
                client_id  INTEGER PRIMARY KEY,
                user_id    INTEGER NOT NULL,
                trainer_id INTEGER NOT NULL,
                weight     REAL,
                height     REAL
            );

            CREATE TABLE IF NOT EXISTS WORKOUT_LOG (
                workout_log_id  INTEGER PRIMARY KEY,
                client_id       INTEGER NOT NULL,
                workout_id      INTEGER,
                date            TEXT NOT NULL,
                total_duration  TEXT,
                type            TEXT NOT NULL DEFAULT 'CUSTOM',
                calories_burned INTEGER,
                rating          INTEGER,
                trainer_notes   TEXT,
                intensity_tag   TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS ACHIEVEMENT (
                achievement_id     INTEGER PRIMARY KEY,
                title              TEXT NOT NULL,
                description        TEXT NOT NULL,
                criteria           TEXT NOT NULL DEFAULT '',
                threshold_workouts INTEGER
            );

            CREATE TABLE IF NOT EXISTS CLIENT_ACHIEVEMENT (
                client_id      INTEGER NOT NULL,
                achievement_id INTEGER NOT NULL,
                unlocked       INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (client_id, achievement_id),
                FOREIGN KEY (client_id)      REFERENCES CLIENT(client_id),
                FOREIGN KEY (achievement_id) REFERENCES ACHIEVEMENT(achievement_id)
            );", connection);
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void GetWorkoutCount_ShouldReturnZero_WhenNoWorkoutsExist()
    {
        InsertTestClient(1);

        var count = this.repository.GetWorkoutCount(1);

        count.Should().Be(0);
    }

    [Fact]
    public void GetWorkoutCount_ShouldReturnCorrectCount_WhenWorkoutsExist()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));

        var count = this.repository.GetWorkoutCount(1);

        count.Should().Be(3);
    }

    [Fact]
    public void GetWorkoutCount_ShouldReturnCountForSpecificClient_WhenMultipleClientsExist()
    {
        InsertTestClient(1);
        InsertTestClient(2);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(2, DateTime.Today);

        var count = this.repository.GetWorkoutCount(1);

        count.Should().Be(2);
    }

    [Fact]
    public void GetDistinctWorkoutDayCount_ShouldReturnZero_WhenNoWorkoutsExist()
    {
        InsertTestClient(1);

        var count = this.repository.GetDistinctWorkoutDayCount(1);

        count.Should().Be(0);
    }

    [Fact]
    public void GetDistinctWorkoutDayCount_ShouldReturnCorrectCount_WhenWorkoutsOnDifferentDays()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));

        var count = this.repository.GetDistinctWorkoutDayCount(1);

        count.Should().Be(3);
    }

    [Fact]
    public void GetDistinctWorkoutDayCount_ShouldCountOnlyUniqueDays_WhenMultipleWorkoutsOnSameDay()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));

        var count = this.repository.GetDistinctWorkoutDayCount(1);

        count.Should().Be(2);
    }

    [Fact]
    public void GetWorkoutsInLastSevenDays_ShouldReturnZero_WhenNoRecentWorkouts()
    {
        InsertTestClient(1);
        var today = DateTime.UtcNow.Date;
        InsertWorkoutLog(1, today.AddDays(-10));
        InsertWorkoutLog(1, today.AddDays(-20));

        var count = this.repository.GetWorkoutsInLastSevenDays(1);

        count.Should().Be(0);
    }

    [Fact]
    public void GetWorkoutsInLastSevenDays_ShouldReturnCorrectCount_WhenWorkoutsInRange()
    {
        InsertTestClient(1);
        var today = DateTime.UtcNow.Date;
        InsertWorkoutLog(1, today);
        InsertWorkoutLog(1, today.AddDays(-1));
        InsertWorkoutLog(1, today.AddDays(-3));
        InsertWorkoutLog(1, today.AddDays(-6));
        InsertWorkoutLog(1, today.AddDays(-10));

        var count = this.repository.GetWorkoutsInLastSevenDays(1);

        count.Should().Be(4);
    }

    [Fact]
    public void GetWorkoutsInLastSevenDays_ShouldIncludeTodaysWorkouts()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.UtcNow.Date);

        var count = this.repository.GetWorkoutsInLastSevenDays(1);

        count.Should().Be(1);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnZero_WhenNoWorkouts()
    {
        InsertTestClient(1);

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);

        streak.Should().Be(0);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnOne_WhenOnlyOneWorkout()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);

        streak.Should().Be(1);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldCalculateCorrectStreak_WhenConsecutiveDays()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-3));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);

        streak.Should().Be(4);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnMaxStreak_WhenMultipleStreaks()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-5));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-6));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-7));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-8));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-9));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);

        streak.Should().Be(5);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldHandleMultipleWorkoutsOnSameDay()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);

        streak.Should().Be(3);
    }

    [Fact]
    public void GetConsecutiveWorkoutDayStreak_ShouldReturnOne_WhenNoConsecutiveDays()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-3));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-7));

        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);

        streak.Should().Be(1);
    }

    [Fact]
    public void GetAllAchievements_ShouldReturnEmptyList_WhenNoAchievementsExist()
    {
        var achievements = this.repository.GetAllAchievements();

        achievements.Should().BeEmpty();
    }

    [Fact]
    public void GetAllAchievements_ShouldReturnAllAchievements_WhenAchievementsExist()
    {
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);
        InsertAchievement(2, "10 Workouts", "Complete 10 workouts", "WORKOUT_COUNT", 10);
        InsertAchievement(3, "100 Workouts", "Complete 100 workouts", "WORKOUT_COUNT", 100);

        var achievements = this.repository.GetAllAchievements();

        achievements.Should().HaveCount(3);
        achievements[0].AchievementId.Should().Be(1);
        achievements[0].Name.Should().Be("First Workout");
        achievements[0].Description.Should().Be("Complete your first workout");
        achievements[0].Criteria.Should().Be("WORKOUT_COUNT");
        achievements[0].ThresholdWorkouts.Should().Be(1);
        achievements[0].IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void GetAllAchievements_ShouldOrderByAchievementId()
    {
        InsertAchievement(3, "Achievement 3", "Description 3", "CRITERIA", null);
        InsertAchievement(1, "Achievement 1", "Description 1", "CRITERIA", null);
        InsertAchievement(2, "Achievement 2", "Description 2", "CRITERIA", null);

        var achievements = this.repository.GetAllAchievements();

        achievements.Should().HaveCount(3);
        achievements[0].AchievementId.Should().Be(1);
        achievements[1].AchievementId.Should().Be(2);
        achievements[2].AchievementId.Should().Be(3);
    }

    [Fact]
    public void GetAllAchievements_ShouldHandleNullThresholdWorkouts()
    {
        InsertAchievement(1, "Special Achievement", "No threshold", "CUSTOM", null);

        var achievements = this.repository.GetAllAchievements();

        achievements.Should().HaveCount(1);
        achievements[0].ThresholdWorkouts.Should().BeNull();
    }

    [Fact]
    public void GetAllAchievements_ShouldHandleEmptyCriteria()
    {
        InsertAchievement(1, "Achievement", "Description", "", 10);

        var achievements = this.repository.GetAllAchievements();

        achievements.Should().HaveCount(1);
        achievements[0].Criteria.Should().Be("");
    }

    [Fact]
    public void AwardAchievement_ShouldReturnTrue_WhenAchievementNotYetAwarded()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);

        var result = this.repository.AwardAchievement(1, 1);

        result.Should().BeTrue();

        var unlocked = GetClientAchievementUnlockedStatus(1, 1);
        unlocked.Should().BeTrue();
    }

    [Fact]
    public void AwardAchievement_ShouldReturnFalse_WhenAchievementAlreadyAwarded()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);
        InsertClientAchievement(1, 1, true);

        var result = this.repository.AwardAchievement(1, 1);

        result.Should().BeFalse();
    }

    [Fact]
    public void AwardAchievement_ShouldUpdateExistingRecord_WhenRecordExistsButNotUnlocked()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);
        InsertClientAchievement(1, 1, false);

        var result = this.repository.AwardAchievement(1, 1);

        result.Should().BeTrue();

        var unlocked = GetClientAchievementUnlockedStatus(1, 1);
        unlocked.Should().BeTrue();
    }

    [Fact]
    public void AwardAchievement_ShouldHandleMultipleAchievementsForSameClient()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Achievement 1", "Description 1", "CRITERIA", 1);
        InsertAchievement(2, "Achievement 2", "Description 2", "CRITERIA", 2);

        this.repository.AwardAchievement(1, 1);
        this.repository.AwardAchievement(1, 2);

        GetClientAchievementUnlockedStatus(1, 1).Should().BeTrue();
        GetClientAchievementUnlockedStatus(1, 2).Should().BeTrue();
    }

    [Fact]
    public void GetAchievementForClient_ShouldReturnNull_WhenAchievementDoesNotExist()
    {
        InsertTestClient(1);

        var achievement = this.repository.GetAchievementForClient(999, 1);

        achievement.Should().BeNull();
    }

    [Fact]
    public void GetAchievementForClient_ShouldReturnAchievementAsLocked_WhenNotUnlocked()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);

        var achievement = this.repository.GetAchievementForClient(1, 1);

        achievement.Should().NotBeNull();
        achievement!.AchievementId.Should().Be(1);
        achievement.Title.Should().Be("First Workout");
        achievement.Description.Should().Be("Complete your first workout");
        achievement.Criteria.Should().Be("WORKOUT_COUNT");
        achievement.IsUnlocked.Should().BeFalse();
        achievement.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void GetAchievementForClient_ShouldReturnAchievementAsUnlocked_WhenUnlocked()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);
        InsertClientAchievement(1, 1, true);

        var achievement = this.repository.GetAchievementForClient(1, 1);

        achievement.Should().NotBeNull();
        achievement!.IsUnlocked.Should().BeTrue();
        achievement.IsLocked.Should().BeFalse();
        achievement.StatusLine.Should().Be("Unlocked");
    }

    [Fact]
    public void GetAchievementForClient_ShouldReturnCorrectStatusForDifferentClients()
    {
        InsertTestClient(1);
        InsertTestClient(2);
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);
        InsertClientAchievement(1, 1, true);

        var achievement1 = this.repository.GetAchievementForClient(1, 1);
        var achievement2 = this.repository.GetAchievementForClient(1, 2);

        achievement1!.IsUnlocked.Should().BeTrue();
        achievement2!.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldReturnEmptyList_WhenNoAchievementsExist()
    {
        InsertTestClient(1);

        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        showcase.Should().BeEmpty();
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldReturnAllAchievements_WhenNoneUnlocked()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Achievement 1", "Description 1", "CRITERIA", 1);
        InsertAchievement(2, "Achievement 2", "Description 2", "CRITERIA", 2);

        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        showcase.Should().HaveCount(2);
        showcase.Should().AllSatisfy(a => a.IsUnlocked.Should().BeFalse());
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldOrderUnlockedFirst()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Achievement 1", "Description 1", "CRITERIA", 1);
        InsertAchievement(2, "Achievement 2", "Description 2", "CRITERIA", 2);
        InsertAchievement(3, "Achievement 3", "Description 3", "CRITERIA", 3);
        InsertClientAchievement(1, 2, true);

        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        showcase.Should().HaveCount(3);
        showcase[0].AchievementId.Should().Be(2);
        showcase[0].IsUnlocked.Should().BeTrue();
        showcase[1].IsUnlocked.Should().BeFalse();
        showcase[2].IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldExcludeDuplicateTitles()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Same Title", "Description 1", "CRITERIA", 1);
        InsertAchievement(2, "Same Title", "Description 2", "CRITERIA", 2);
        InsertAchievement(3, "Different Title", "Description 3", "CRITERIA", 3);

        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        showcase.Should().HaveCount(2);
        showcase.Should().ContainSingle(a => a.Title == "Same Title");
        showcase.Should().ContainSingle(a => a.Title == "Different Title");
    }

    [Fact]
    public void GetAchievementShowcaseForClient_ShouldHandleMixedUnlockedStatus()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Achievement 1", "Description 1", "CRITERIA", 1);
        InsertAchievement(2, "Achievement 2", "Description 2", "CRITERIA", 2);
        InsertAchievement(3, "Achievement 3", "Description 3", "CRITERIA", 3);
        InsertClientAchievement(1, 1, true);
        InsertClientAchievement(1, 3, true);

        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        showcase.Should().HaveCount(3);
        var unlocked = showcase.Where(a => a.IsUnlocked).ToList();
        var locked = showcase.Where(a => !a.IsUnlocked).ToList();
        unlocked.Should().HaveCount(2);
        locked.Should().HaveCount(1);
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldUnlockAppropriateAchievements()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete 1 workout", "WORKOUT_COUNT", 1);
        InsertAchievement(2, "5 Workouts", "Complete 5 workouts", "WORKOUT_COUNT", 5);
        InsertAchievement(3, "10 Workouts", "Complete 10 workouts", "WORKOUT_COUNT", 10);

        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-3));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-4));

        this.repository.EvaluateAndUnlockWorkoutMilestones(1);

        GetClientAchievementUnlockedStatus(1, 1).Should().BeTrue();
        GetClientAchievementUnlockedStatus(1, 2).Should().BeTrue();
        HasClientAchievement(1, 3).Should().BeFalse();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldNotUnlock_WhenThresholdNotMet()
    {
        InsertTestClient(1);
        InsertAchievement(1, "10 Workouts", "Complete 10 workouts", "WORKOUT_COUNT", 10);

        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));

        this.repository.EvaluateAndUnlockWorkoutMilestones(1);

        HasClientAchievement(1, 1).Should().BeFalse();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldNotDuplicateUnlocks()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete 1 workout", "WORKOUT_COUNT", 1);

        InsertWorkoutLog(1, DateTime.Today);

        this.repository.EvaluateAndUnlockWorkoutMilestones(1);
        this.repository.EvaluateAndUnlockWorkoutMilestones(1);

        var count = GetClientAchievementCount(1, 1);
        count.Should().Be(1);
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldSkipAchievementsWithoutThreshold()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Special Achievement", "No threshold", "CUSTOM", null);
        InsertAchievement(2, "First Workout", "Complete 1 workout", "WORKOUT_COUNT", 1);

        InsertWorkoutLog(1, DateTime.Today);

        this.repository.EvaluateAndUnlockWorkoutMilestones(1);

        HasClientAchievement(1, 1).Should().BeFalse();
        GetClientAchievementUnlockedStatus(1, 2).Should().BeTrue();
    }

    [Fact]
    public void EvaluateAndUnlockWorkoutMilestones_ShouldHandleNoWorkouts()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete 1 workout", "WORKOUT_COUNT", 1);

        this.repository.EvaluateAndUnlockWorkoutMilestones(1);

        HasClientAchievement(1, 1).Should().BeFalse();
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainDataIntegrity()
    {
        InsertTestClient(1);
        InsertAchievement(1, "First Workout", "Complete 1 workout", "WORKOUT_COUNT", 1);
        InsertAchievement(2, "3 Day Streak", "Workout 3 days in a row", "STREAK", 3);

        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));

        this.repository.EvaluateAndUnlockWorkoutMilestones(1);

        var count = this.repository.GetWorkoutCount(1);
        var distinctDays = this.repository.GetDistinctWorkoutDayCount(1);
        var streak = this.repository.GetConsecutiveWorkoutDayStreak(1);
        var recentWorkouts = this.repository.GetWorkoutsInLastSevenDays(1);
        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        count.Should().Be(3);
        distinctDays.Should().Be(3);
        streak.Should().Be(3);
        recentWorkouts.Should().Be(3);
        showcase.Should().HaveCount(2);
    }

    private void InsertTestClient(int clientId)
    {
        using var cmd = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@id, @uid, 1, 75.0, 180.0)",
            this.connection);
        cmd.Parameters.AddWithValue("@id", clientId);
        cmd.Parameters.AddWithValue("@uid", clientId + 1000);
        cmd.ExecuteNonQuery();
    }

    private void InsertWorkoutLog(int clientId, DateTime date)
    {
        using var cmd = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG (client_id, date, type, calories_burned, intensity_tag)
              VALUES (@cid, @date, 'CUSTOM', 300, 'moderate')",
            this.connection);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@date", date.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    private void InsertAchievement(int achievementId, string title, string description, string criteria, int? thresholdWorkouts)
    {
        using var cmd = new SqliteCommand(
            @"INSERT INTO ACHIEVEMENT (achievement_id, title, description, criteria, threshold_workouts)
              VALUES (@id, @title, @desc, @criteria, @threshold)",
            this.connection);
        cmd.Parameters.AddWithValue("@id", achievementId);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@desc", description);
        cmd.Parameters.AddWithValue("@criteria", criteria);
        cmd.Parameters.AddWithValue("@threshold", thresholdWorkouts ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private void InsertClientAchievement(int clientId, int achievementId, bool unlocked)
    {
        using var cmd = new SqliteCommand(
            @"INSERT INTO CLIENT_ACHIEVEMENT (client_id, achievement_id, unlocked)
              VALUES (@cid, @aid, @unlocked)",
            this.connection);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@aid", achievementId);
        cmd.Parameters.AddWithValue("@unlocked", unlocked ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    private bool GetClientAchievementUnlockedStatus(int clientId, int achievementId)
    {
        using var cmd = new SqliteCommand(
            @"SELECT unlocked FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @cid AND achievement_id = @aid",
            this.connection);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@aid", achievementId);
        var result = cmd.ExecuteScalar();
        return result != null && Convert.ToInt32(result) == 1;
    }

    private bool HasClientAchievement(int clientId, int achievementId)
    {
        using var cmd = new SqliteCommand(
            @"SELECT COUNT(1) FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @cid AND achievement_id = @aid",
            this.connection);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@aid", achievementId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private int GetClientAchievementCount(int clientId, int achievementId)
    {
        using var cmd = new SqliteCommand(
            @"SELECT COUNT(1) FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @cid AND achievement_id = @aid",
            this.connection);
        cmd.Parameters.AddWithValue("@cid", clientId);
        cmd.Parameters.AddWithValue("@aid", achievementId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}

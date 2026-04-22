using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryAchievementsTests : IDisposable
{
    private const int UserIdOffset = 1000;
    private const int DefaultTrainerId = 1;
    private const double DefaultWeight = 75.0;
    private const double DefaultHeight = 180.0;
    private const int DefaultCaloriesBurned = 300;
    private const int DatabaseBooleanFalse = 0;
    private const int DatabaseBooleanTrue = 1;
    private const int NonExistentAchievementId = 999;
    private const int TenDaysAgo = -10;
    private const int TwentyDaysAgo = -20;
    private const int ExpectedCountZero = 0;
    private const int ExpectedCountOne = 1;
    private const int ExpectedCountTwo = 2;
    private const int ExpectedCountThree = 3;
    private const int ExpectedCountFour = 4;
    private const int ExpectedCountFive = 5;

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
        using var command = new SqliteCommand(
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
        command.ExecuteNonQuery();
    }

    [Fact]
    public void GetWorkoutCount_ShouldReturnCorrectCount_WhenWorkoutsExist()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));

        var count = this.repository.GetWorkoutCount(1);

        count.Should().Be(ExpectedCountThree);
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

        count.Should().Be(ExpectedCountTwo);
    }

    [Fact]
    public void GetDistinctWorkoutDayCount_ShouldReturnCorrectCount_WhenWorkoutsOnDifferentDays()
    {
        InsertTestClient(1);
        InsertWorkoutLog(1, DateTime.Today);
        InsertWorkoutLog(1, DateTime.Today.AddDays(-1));
        InsertWorkoutLog(1, DateTime.Today.AddDays(-2));

        var count = this.repository.GetDistinctWorkoutDayCount(1);

        count.Should().Be(ExpectedCountThree);
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

        count.Should().Be(ExpectedCountTwo);
    }

    [Fact]
    public void GetWorkoutsInLastSevenDays_ShouldReturnZero_WhenNoRecentWorkouts()
    {
        InsertTestClient(1);
        var today = DateTime.UtcNow.Date;
        InsertWorkoutLog(1, today.AddDays(TenDaysAgo));
        InsertWorkoutLog(1, today.AddDays(TwentyDaysAgo));

        var count = this.repository.GetWorkoutsInLastSevenDays(1);

        count.Should().Be(ExpectedCountZero);
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

        count.Should().Be(ExpectedCountFour);
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

        streak.Should().Be(ExpectedCountFour);
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

        streak.Should().Be(ExpectedCountFive);
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

        streak.Should().Be(ExpectedCountThree);
    }

    [Fact]
    public void GetAllAchievements_ShouldReturnAllAchievements_WhenAchievementsExist()
    {
        InsertAchievement(1, "First Workout", "Complete your first workout", "WORKOUT_COUNT", 1);
        InsertAchievement(2, "10 Workouts", "Complete 10 workouts", "WORKOUT_COUNT", 10);
        InsertAchievement(3, "100 Workouts", "Complete 100 workouts", "WORKOUT_COUNT", 100);

        var achievements = this.repository.GetAllAchievements();

        achievements.Should().HaveCount(ExpectedCountThree);
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

        achievements.Should().HaveCount(ExpectedCountThree);
        achievements[0].AchievementId.Should().Be(1);
        achievements[1].AchievementId.Should().Be(2);
        achievements[2].AchievementId.Should().Be(3);
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
    public void GetAchievementForClient_ShouldReturnNull_WhenAchievementDoesNotExist()
    {
        InsertTestClient(1);

        var achievement = this.repository.GetAchievementForClient(NonExistentAchievementId, 1);

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
    public void GetAchievementShowcaseForClient_ShouldOrderUnlockedFirst()
    {
        InsertTestClient(1);
        InsertAchievement(1, "Achievement 1", "Description 1", "CRITERIA", 1);
        InsertAchievement(2, "Achievement 2", "Description 2", "CRITERIA", 2);
        InsertAchievement(3, "Achievement 3", "Description 3", "CRITERIA", 3);
        InsertClientAchievement(1, 2, true);

        var showcase = this.repository.GetAchievementShowcaseForClient(1);

        showcase.Should().HaveCount(ExpectedCountThree);
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

        showcase.Should().HaveCount(ExpectedCountTwo);
        showcase.Should().ContainSingle(a => a.Title == "Same Title");
        showcase.Should().ContainSingle(a => a.Title == "Different Title");
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
        count.Should().Be(ExpectedCountOne);
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

        count.Should().Be(ExpectedCountThree);
        distinctDays.Should().Be(ExpectedCountThree);
        streak.Should().Be(ExpectedCountThree);
        recentWorkouts.Should().Be(ExpectedCountThree);
        showcase.Should().HaveCount(ExpectedCountTwo);
    }

    private void InsertTestClient(int clientId)
    {
        using var command = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@identifier, @userId, @trainerId, @weight, @height)",
            this.connection);
        command.Parameters.AddWithValue("@identifier", clientId);
        command.Parameters.AddWithValue("@userId", clientId + UserIdOffset);
        command.Parameters.AddWithValue("@trainerId", DefaultTrainerId);
        command.Parameters.AddWithValue("@weight", DefaultWeight);
        command.Parameters.AddWithValue("@height", DefaultHeight);
        command.ExecuteNonQuery();
    }

    private void InsertWorkoutLog(int clientId, DateTime date)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG (client_id, date, type, calories_burned, intensity_tag)
              VALUES (@clientId, @date, 'CUSTOM', @caloriesBurned, 'moderate')",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@date", date.ToString("o"));
        command.Parameters.AddWithValue("@caloriesBurned", DefaultCaloriesBurned);
        command.ExecuteNonQuery();
    }

    private void InsertAchievement(int achievementId, string title, string description, string criteria, int? thresholdWorkouts)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO ACHIEVEMENT (achievement_id, title, description, criteria, threshold_workouts)
              VALUES (@identifier, @title, @description, @criteria, @threshold)",
            this.connection);
        command.Parameters.AddWithValue("@identifier", achievementId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@criteria", criteria);
        command.Parameters.AddWithValue("@threshold", thresholdWorkouts ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    private void InsertClientAchievement(int clientId, int achievementId, bool unlocked)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO CLIENT_ACHIEVEMENT (client_id, achievement_id, unlocked)
              VALUES (@clientId, @achievementId, @unlocked)",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@achievementId", achievementId);
        command.Parameters.AddWithValue("@unlocked", unlocked ? DatabaseBooleanTrue : DatabaseBooleanFalse);
        command.ExecuteNonQuery();
    }

    private bool GetClientAchievementUnlockedStatus(int clientId, int achievementId)
    {
        using var command = new SqliteCommand(
            @"SELECT unlocked FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @clientId AND achievement_id = @achievementId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@achievementId", achievementId);
        var result = command.ExecuteScalar();
        return result != null && Convert.ToInt32(result) == DatabaseBooleanTrue;
    }

    private bool HasClientAchievement(int clientId, int achievementId)
    {
        using var command = new SqliteCommand(
            @"SELECT COUNT(1) FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @clientId AND achievement_id = @achievementId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@achievementId", achievementId);
        return Convert.ToInt32(command.ExecuteScalar()) > ExpectedCountZero;
    }

    private int GetClientAchievementCount(int clientId, int achievementId)
    {
        using var command = new SqliteCommand(
            @"SELECT COUNT(1) FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @clientId AND achievement_id = @achievementId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@achievementId", achievementId);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}

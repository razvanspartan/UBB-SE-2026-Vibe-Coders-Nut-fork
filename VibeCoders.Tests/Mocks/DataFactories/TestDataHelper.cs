namespace VibeCoders.Tests.Mocks.DataFactories;

using Microsoft.Data.Sqlite;

public class TestDataHelper
{
    private const int UserIdOffset = 1000;
    private const int DefaultTrainerId = 1;
    private const double DefaultWeight = 75.0;
    private const double DefaultHeight = 180.0;
    private const int DefaultCaloriesBurned = 300;
    private const int DatabaseBooleanFalse = 0;
    private const int DatabaseBooleanTrue = 1;

    private readonly SqliteConnection connection;

    public TestDataHelper(SqliteConnection connection)
    {
        this.connection = connection;
    }

    public void SetupTrainer()
    {
        using var trainerUserCommand = new SqliteCommand(
            @"INSERT INTO ""USER"" (id, username, password_hash, role) VALUES (@userId, @username, @passwordHash, 'TRAINER')",
            this.connection);
        trainerUserCommand.Parameters.AddWithValue("@userId", DefaultTrainerId);
        trainerUserCommand.Parameters.AddWithValue("@username", "testtrainer");
        trainerUserCommand.Parameters.AddWithValue("@passwordHash", "hash456");
        trainerUserCommand.ExecuteNonQuery();

        using var trainerCommand = new SqliteCommand(
            "INSERT INTO TRAINER (trainer_id, user_id) VALUES (@trainerId, @userId)",
            this.connection);
        trainerCommand.Parameters.AddWithValue("@trainerId", DefaultTrainerId);
        trainerCommand.Parameters.AddWithValue("@userId", DefaultTrainerId);
        trainerCommand.ExecuteNonQuery();
    }

    public void InsertClient(int clientId)
    {
        using var userCommand = new SqliteCommand(
            @"INSERT INTO ""USER"" (id, username, password_hash, role) VALUES (@userId, @username, @passwordHash, 'CLIENT')",
            this.connection);
        userCommand.Parameters.AddWithValue("@userId", clientId + UserIdOffset);
        userCommand.Parameters.AddWithValue("@username", $"testuser{clientId}");
        userCommand.Parameters.AddWithValue("@passwordHash", "hash123");
        userCommand.ExecuteNonQuery();

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

    public void InsertWorkoutLog(int clientId, DateTime date)
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

    public void InsertAchievement(int achievementId, string title, string description, string criteria, int? thresholdWorkouts)
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

    public void InsertClientAchievement(int clientId, int achievementId, bool unlocked)
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

    public bool GetClientAchievementUnlockedStatus(int clientId, int achievementId)
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

    public bool HasClientAchievement(int clientId, int achievementId)
    {
        using var command = new SqliteCommand(
            @"SELECT COUNT(1) FROM CLIENT_ACHIEVEMENT 
              WHERE client_id = @clientId AND achievement_id = @achievementId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@achievementId", achievementId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int GetClientAchievementCount(int clientId, int achievementId)
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

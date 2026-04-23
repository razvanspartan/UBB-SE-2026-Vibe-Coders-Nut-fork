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

    public void InsertNotification(int clientId, string title, string message, string type, int relatedId, DateTime dateCreated, bool isRead)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO NOTIFICATION (client_id, title, message, type, related_id, date_created, is_read)
              VALUES (@clientId, @title, @message, @type, @relatedId, @dateCreated, @isRead)",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@message", message);
        command.Parameters.AddWithValue("@type", type);
        command.Parameters.AddWithValue("@relatedId", relatedId);
        command.Parameters.AddWithValue("@dateCreated", dateCreated.ToString("o"));
        command.Parameters.AddWithValue("@isRead", isRead ? DatabaseBooleanTrue : DatabaseBooleanFalse);
        command.ExecuteNonQuery();
    }

    public int GetNotificationCount(int clientId)
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM NOTIFICATION WHERE client_id = @clientId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public int GetTotalNotificationCount()
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM NOTIFICATION",
            this.connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void InsertWorkoutTemplate(int templateId, int clientId, string name)
    {
        using var command = new SqliteCommand(
            "INSERT INTO WORKOUT_TEMPLATE (workout_template_id, client_id, name, type) VALUES (@templateId, @clientId, @name, 'CUSTOM')",
            this.connection);
        command.Parameters.AddWithValue("@templateId", templateId);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@name", name);
        command.ExecuteNonQuery();
    }

    public int InsertWorkoutLog(int clientId, int workoutId, DateOnly date, string? duration, int calories, string intensity)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG (client_id, workout_id, date, total_duration, calories_burned, intensity_tag, type)
              VALUES (@clientId, @workoutId, @date, @duration, @calories, @intensity, 'CUSTOM');
              SELECT last_insert_rowid();",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@workoutId", workoutId);
        command.Parameters.AddWithValue("@date", date.ToString("o"));
        command.Parameters.AddWithValue("@duration", duration ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@calories", calories);
        command.Parameters.AddWithValue("@intensity", intensity);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void InsertWorkoutLogSet(int logId, string exerciseName, int setIndex, int? reps, double? weight,
        int? targetReps, double? targetWeight, double performanceRatio, bool isSystemAdjusted, string? adjustmentNote)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG_SETS 
              (workout_log_id, exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note)
              VALUES (@logId, @exerciseName, @setIndex, @reps, @weight, @targetReps, @targetWeight, @performanceRatio, @isSystemAdjusted, @adjustmentNote)",
            this.connection);
        command.Parameters.AddWithValue("@logId", logId);
        command.Parameters.AddWithValue("@exerciseName", exerciseName);
        command.Parameters.AddWithValue("@setIndex", setIndex);
        command.Parameters.AddWithValue("@reps", reps ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@weight", weight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@targetReps", targetReps ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@targetWeight", targetWeight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@performanceRatio", performanceRatio);
        command.Parameters.AddWithValue("@isSystemAdjusted", isSystemAdjusted ? DatabaseBooleanTrue : DatabaseBooleanFalse);
        command.Parameters.AddWithValue("@adjustmentNote", adjustmentNote ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }
}

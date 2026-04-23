using Microsoft.Data.Sqlite;

namespace VibeCoders.Tests.Mocks.DataFactories;

public sealed class RepositoryWorkoutLogDataFactory
{
    private const string DefaultWorkoutTemplateName = "Strength Day";
    private const string CustomWorkoutType = "CUSTOM";
    private const int FirstSetIndex = 0;

    private readonly SqliteConnection connection;

    public RepositoryWorkoutLogDataFactory(SqliteConnection connection)
    {
        this.connection = connection;
    }

    public void InsertWorkoutLogWithDuration(int clientId, DateTime workoutDate, string? totalDuration)
    {
        using var command = new SqliteCommand(
                        @"INSERT INTO WORKOUT_LOG (client_id, date, total_duration, type)
                            VALUES (@clientId, @date, @totalDuration, @type)",
            this.connection);

        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@date", workoutDate.ToString("o"));
        command.Parameters.AddWithValue("@totalDuration", totalDuration is null ? DBNull.Value : totalDuration);
                command.Parameters.AddWithValue("@type", CustomWorkoutType);

        command.ExecuteNonQuery();
    }

    public void InsertWorkoutTemplate(int workoutTemplateIdentifier, int clientId)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_TEMPLATE (workout_template_id, client_id, name, type)
              VALUES (@workoutTemplateIdentifier, @clientId, @name, @type)",
            this.connection);

        command.Parameters.AddWithValue("@workoutTemplateIdentifier", workoutTemplateIdentifier);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@name", DefaultWorkoutTemplateName);
        command.Parameters.AddWithValue("@type", CustomWorkoutType);

        command.ExecuteNonQuery();
    }

    public void InsertTemplateExercise(int templateExerciseIdentifier, int workoutTemplateIdentifier, string exerciseName, string muscleGroup)
    {
        using var command = new SqliteCommand(
                        @"INSERT INTO TEMPLATE_EXERCISE (id, workout_template_id, name, muscle_group)
                            VALUES (@templateExerciseIdentifier, @workoutTemplateIdentifier, @exerciseName, @muscleGroup)",
            this.connection);

        command.Parameters.AddWithValue("@templateExerciseIdentifier", templateExerciseIdentifier);
        command.Parameters.AddWithValue("@workoutTemplateIdentifier", workoutTemplateIdentifier);
        command.Parameters.AddWithValue("@exerciseName", exerciseName);
        command.Parameters.AddWithValue("@muscleGroup", muscleGroup);

        command.ExecuteNonQuery();
    }

    public int InsertWorkoutLogWithExerciseSet(int clientId, int workoutTemplateIdentifier, DateTime workoutDate, string totalDuration, string exerciseName)
    {
        using var insertWorkoutLogCommand = new SqliteCommand(
                        @"INSERT INTO WORKOUT_LOG (client_id, workout_id, date, total_duration, type)
                            VALUES (@clientId, @workoutTemplateIdentifier, @date, @totalDuration, @type)",
            this.connection);

        insertWorkoutLogCommand.Parameters.AddWithValue("@clientId", clientId);
        insertWorkoutLogCommand.Parameters.AddWithValue("@workoutTemplateIdentifier", workoutTemplateIdentifier);
        insertWorkoutLogCommand.Parameters.AddWithValue("@date", workoutDate.ToString("o"));
        insertWorkoutLogCommand.Parameters.AddWithValue("@totalDuration", totalDuration);
        insertWorkoutLogCommand.Parameters.AddWithValue("@type", CustomWorkoutType);
        insertWorkoutLogCommand.ExecuteNonQuery();

        using var insertedIdentifierCommand = new SqliteCommand("SELECT last_insert_rowid();", this.connection);
        var workoutLogIdentifier = Convert.ToInt32(insertedIdentifierCommand.ExecuteScalar());

        using var insertWorkoutLogSetCommand = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG_SETS
                            (workout_log_id, exercise_name, sets)
              VALUES
                            (@workoutLogIdentifier, @exerciseName, @setIndex)",
            this.connection);

        insertWorkoutLogSetCommand.Parameters.AddWithValue("@workoutLogIdentifier", workoutLogIdentifier);
        insertWorkoutLogSetCommand.Parameters.AddWithValue("@exerciseName", exerciseName);
        insertWorkoutLogSetCommand.Parameters.AddWithValue("@setIndex", FirstSetIndex);
        insertWorkoutLogSetCommand.ExecuteNonQuery();

        return workoutLogIdentifier;
    }
}
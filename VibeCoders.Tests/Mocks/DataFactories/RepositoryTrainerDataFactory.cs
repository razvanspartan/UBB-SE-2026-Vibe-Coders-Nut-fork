using Microsoft.Data.Sqlite;
using VibeCoders.Models;

namespace VibeCoders.Tests.Mocks.DataFactories;

public sealed class RepositoryTrainerDataFactory
{
    private const string CustomWorkoutType = "CUSTOM";

    private readonly SqliteConnection connection;

    public RepositoryTrainerDataFactory(SqliteConnection connection)
    {
        this.connection = connection;
    }

    public void InsertWorkoutLog(int clientId, DateTime workoutDate)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO WORKOUT_LOG (client_id, date, type)
              VALUES (@clientId, @date, @type)",
            this.connection);

        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@date", workoutDate.ToString("o"));
        command.Parameters.AddWithValue("@type", CustomWorkoutType);

        command.ExecuteNonQuery();
    }

    public string GetWorkoutTemplateName(int workoutTemplateIdentifier)
    {
        using var command = new SqliteCommand(
            @"SELECT name
              FROM WORKOUT_TEMPLATE
              WHERE workout_template_id = @workoutTemplateIdentifier",
            this.connection);

        command.Parameters.AddWithValue("@workoutTemplateIdentifier", workoutTemplateIdentifier);

        return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
    }

    public List<string> GetTemplateExerciseNames(int workoutTemplateIdentifier)
    {
        var exerciseNames = new List<string>();

        using var command = new SqliteCommand(
            @"SELECT name
              FROM TEMPLATE_EXERCISE
              WHERE workout_template_id = @workoutTemplateIdentifier
              ORDER BY name",
            this.connection);

        command.Parameters.AddWithValue("@workoutTemplateIdentifier", workoutTemplateIdentifier);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            exerciseNames.Add(reader.GetString(0));
        }

        return exerciseNames;
    }

    public static WorkoutTemplate CreateWorkoutTemplate(int clientId, string workoutName, string exerciseName, MuscleGroup muscleGroup)
    {
        var workoutTemplate = new WorkoutTemplate
        {
            ClientId = clientId,
            Name = workoutName,
            Type = WorkoutType.CUSTOM,
        };

        workoutTemplate.AddExercise(new TemplateExercise
        {
            Name = exerciseName,
            MuscleGroup = muscleGroup,
        });

        return workoutTemplate;
    }
}
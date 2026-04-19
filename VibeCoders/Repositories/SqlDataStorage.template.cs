namespace VibeCoders.Services
{
    using Microsoft.Data.Sqlite;
    using VibeCoders.Models;

    public partial class SqlDataStorage
    {
        private static WorkoutType ParseWorkoutType(string? value)
        {
            if (string.Equals(value, "PRE_BUILT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "PREBUILT", StringComparison.OrdinalIgnoreCase))
            {
                return WorkoutType.PREBUILT;
            }

            if (string.Equals(value, "TRAINERASSIGNED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "TRAINER-ASSIGNED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "TRAINER ASSIGNED", StringComparison.OrdinalIgnoreCase))
            {
                return WorkoutType.TRAINER_ASSIGNED;
            }

            return Enum.TryParse<WorkoutType>(value, true, out var parsed)
                ? parsed
                : WorkoutType.CUSTOM;
        }

        private static string SerializeWorkoutType(WorkoutType type)
        {
            return type == WorkoutType.PREBUILT ? "PRE_BUILT" : type.ToString();
        }

        public List<WorkoutTemplate> GetAvailableWorkouts(int clientId)
        {
            const string sql = @"
                SELECT
                    wt.workout_template_id,
                    wt.client_id,
                    wt.name,
                    wt.type
                FROM WORKOUT_TEMPLATE wt
                WHERE UPPER(REPLACE(REPLACE(wt.type, '_', ''), '-', '')) = 'PREBUILT'
                   OR (UPPER(REPLACE(REPLACE(wt.type, '_', ''), '-', '')) = 'TRAINERASSIGNED' AND wt.client_id = @ClientId)
                   OR (UPPER(REPLACE(REPLACE(wt.type, '_', ''), '-', '')) = 'CUSTOM' AND wt.client_id = @ClientId)
                ORDER BY wt.type, wt.name;";

            var templates = new List<WorkoutTemplate>();

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    templates.Add(new WorkoutTemplate
                    {
                        Id = reader.GetInt32(0),
                        ClientId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Type = ParseWorkoutType(reader.GetString(3)),
                    });
                }
            }

            foreach (var template in templates)
            {
                var exercises = this.LoadExercisesForTemplate(template.Id, connection);
                foreach (var exercise in exercises)
                {
                    template.AddExercise(exercise);
                }
            }

            return templates;
        }

        public TemplateExercise? GetTemplateExercise(int templateExerciseId)
        {
            const string sql = @"
                SELECT
                    id,
                    name,
                    workout_template_id,
                    muscle_group,
                    target_sets,
                    target_reps,
                    target_weight
                FROM TEMPLATE_EXERCISE
                WHERE id = @Id;";

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", templateExerciseId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new TemplateExercise
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                WorkoutTemplateId = reader.GetInt32(2),
                MuscleGroup = Enum.Parse<MuscleGroup>(reader.GetString(3)),
                TargetSets = reader.GetInt32(4),
                TargetReps = reader.GetInt32(5),
                TargetWeight = reader.GetDouble(6),
            };
        }

        public bool UpdateTemplateWeight(int templateExerciseId, double newWeight)
        {
            const string sql = @"
                UPDATE TEMPLATE_EXERCISE
                SET target_weight = @NewWeight
                WHERE id = @Id;";

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@NewWeight", newWeight);
            command.Parameters.AddWithValue("@Id", templateExerciseId);

            return command.ExecuteNonQuery() > 0;
        }

        public List<string> GetAllExerciseNames()
        {
            const string sql = "SELECT name FROM EXERCISE ORDER BY name ASC;";
            var list = new List<string>();

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }

            return list;
        }

        private List<TemplateExercise> LoadExercisesForTemplate(int templateId, SqliteConnection connection)
        {
            const string sql = @"
                SELECT
                    id,
                    workout_template_id,
                    name,
                    muscle_group,
                    target_sets,
                    target_reps,
                    target_weight
                FROM TEMPLATE_EXERCISE
                WHERE workout_template_id = @TemplateId
                ORDER BY id;";

            var exercises = new List<TemplateExercise>();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@TemplateId", templateId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                exercises.Add(new TemplateExercise
                {
                    Id = reader.GetInt32(0),
                    WorkoutTemplateId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    MuscleGroup = Enum.Parse<MuscleGroup>(reader.GetString(3)),
                    TargetSets = reader.GetInt32(4),
                    TargetReps = reader.GetInt32(5),
                    TargetWeight = reader.GetDouble(6),
                });
            }

            return exercises;
        }
    }
}

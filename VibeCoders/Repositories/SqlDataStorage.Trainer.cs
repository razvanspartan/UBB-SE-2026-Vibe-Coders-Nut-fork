namespace VibeCoders.Services
{
    using Microsoft.Data.Sqlite;
    using VibeCoders.Models;
    using User = VibeCoders.Models.User;

    public partial class SqlDataStorage : IDataStorage
    {
        public List<Client> GetTrainerClient(int trainerId)
        {
            var roster = new List<Client>();

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            string sql = @"
                SELECT
                    c.client_id,
                    u.username,
                    c.weight,
                    c.height,
                    (SELECT MAX(date) FROM WORKOUT_LOG wl WHERE wl.client_id = c.client_id) AS LastWorkoutDate
                FROM CLIENT c
                JOIN ""USER"" u ON c.user_id = u.id
                WHERE c.trainer_id = @TrainerId;";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@TrainerId", trainerId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var client = new Client
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Weight = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                    Height = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                    WorkoutLog = new List<WorkoutLog>(),
                };

                if (!reader.IsDBNull(4))
                {
                    client.WorkoutLog.Add(new WorkoutLog { Date = DateTime.Parse(reader.GetString(4)) });
                }

                roster.Add(client);
            }

            return roster;
        }

        public bool SaveTrainerWorkout(WorkoutTemplate template)
        {
            const string insertTemplateSql = @"
                INSERT INTO WORKOUT_TEMPLATE (client_id, name, type)
                VALUES (@ClientId, @Name, @Type);";

            const string updateTemplateSql = @"
                UPDATE WORKOUT_TEMPLATE
                SET name = @Name, type = @Type
                WHERE workout_template_id = @TemplateId;";

            const string deleteOldExercisesSql = @"
                DELETE FROM TEMPLATE_EXERCISE
                WHERE workout_template_id = @TemplateId;";

            const string insertExerciseSql = @"
                INSERT INTO TEMPLATE_EXERCISE
                    (workout_template_id, name, muscle_group, target_sets, target_reps, target_weight)
                VALUES
                    (@TemplateId, @Name, @MuscleGroup, @TargetSets, @TargetReps, @TargetWeight);";

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                int templateId = template.Id;

                if (templateId == 0)
                {
                    using var command = new SqliteCommand(insertTemplateSql, connection, transaction);
                    command.Parameters.AddWithValue("@ClientId", template.ClientId);
                    command.Parameters.AddWithValue("@Name", template.Name);
                    command.Parameters.AddWithValue("@Type", SerializeWorkoutType(template.Type));
                    command.ExecuteNonQuery();

                    using var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection, transaction);
                    templateId = Convert.ToInt32(idCmd.ExecuteScalar());
                    template.Id = templateId;
                }
                else
                {
                    using var command = new SqliteCommand(updateTemplateSql, connection, transaction);
                    command.Parameters.AddWithValue("@TemplateId", templateId);
                    command.Parameters.AddWithValue("@Name", template.Name);
                    command.Parameters.AddWithValue("@Type", SerializeWorkoutType(template.Type));
                    command.ExecuteNonQuery();

                    using var deleteCmd = new SqliteCommand(deleteOldExercisesSql, connection, transaction);
                    deleteCmd.Parameters.AddWithValue("@TemplateId", templateId);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (var exercise in template.GetExercises())
                {
                    using var command = new SqliteCommand(insertExerciseSql, connection, transaction);
                    command.Parameters.AddWithValue("@TemplateId", templateId);
                    command.Parameters.AddWithValue("@Name", exercise.Name);
                    command.Parameters.AddWithValue("@MuscleGroup", exercise.MuscleGroup.ToString());
                    command.Parameters.AddWithValue("@TargetSets", exercise.TargetSets);
                    command.Parameters.AddWithValue("@TargetReps", exercise.TargetReps);
                    command.Parameters.AddWithValue("@TargetWeight", exercise.TargetWeight);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"Failed to save trainer workout: {exception.Message}");
                return false;
            }
        }

        public bool DeleteWorkoutTemplate(int templateId)
        {
            const string deleteExercisesSql = "DELETE FROM TEMPLATE_EXERCISE WHERE workout_template_id = @Id;";
            const string deleteTemplateSql = "DELETE FROM WORKOUT_TEMPLATE WHERE workout_template_id = @Id;";

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using (var command = new SqliteCommand(deleteExercisesSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Id", templateId);
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(deleteTemplateSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Id", templateId);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool SaveUser(User u)
        {
            return false;
        }

        public User? LoadUser(string username)
        {
            return null;
        }

        public bool SaveClientData(Client c)
        {
            return false;
        }
    }
}

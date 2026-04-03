using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using User = VibeCoders.Models.User;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage : IDataStorage
    {
        public List<Client> GetTrainerClient(int trainerId)
        {
            var roster = new List<Client>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

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

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TrainerId", trainerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var client = new Client
                {
                    Id       = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Weight   = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                    Height   = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                    WorkoutLog = new List<WorkoutLog>()
                };

                if (!reader.IsDBNull(4))
                    client.WorkoutLog.Add(new WorkoutLog { Date = DateTime.Parse(reader.GetString(4)) });

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

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                int templateId = template.Id;

                if (templateId == 0)
                {
                    using var cmd = new SqliteCommand(insertTemplateSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@ClientId", template.ClientId);
                    cmd.Parameters.AddWithValue("@Name", template.Name);
                    cmd.Parameters.AddWithValue("@Type", template.Type.ToString());
                    cmd.ExecuteNonQuery();

                    using var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, transaction);
                    templateId = Convert.ToInt32(idCmd.ExecuteScalar());
                    template.Id = templateId;
                }
                else
                {
                    using var cmd = new SqliteCommand(updateTemplateSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@TemplateId", templateId);
                    cmd.Parameters.AddWithValue("@Name", template.Name);
                    cmd.Parameters.AddWithValue("@Type", template.Type.ToString());
                    cmd.ExecuteNonQuery();

                    using var deleteCmd = new SqliteCommand(deleteOldExercisesSql, conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@TemplateId", templateId);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (var exercise in template.GetExercises())
                {
                    using var cmd = new SqliteCommand(insertExerciseSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@TemplateId", templateId);
                    cmd.Parameters.AddWithValue("@Name", exercise.Name);
                    cmd.Parameters.AddWithValue("@MuscleGroup", exercise.MuscleGroup.ToString());
                    cmd.Parameters.AddWithValue("@TargetSets", exercise.TargetSets);
                    cmd.Parameters.AddWithValue("@TargetReps", exercise.TargetReps);
                    cmd.Parameters.AddWithValue("@TargetWeight", exercise.TargetWeight);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"Failed to save trainer workout: {ex.Message}");
                return false;
            }
        }

        public bool DeleteWorkoutTemplate(int templateId)
        {
            const string deleteExercisesSql = "DELETE FROM TEMPLATE_EXERCISE WHERE workout_template_id = @Id;";
            const string deleteTemplateSql  = "DELETE FROM WORKOUT_TEMPLATE WHERE workout_template_id = @Id;";

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqliteCommand(deleteExercisesSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", templateId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqliteCommand(deleteTemplateSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", templateId);
                    cmd.ExecuteNonQuery();
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
using Microsoft.Data.SqlClient;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        // ── WORKOUT_TEMPLATE ─────────────────────────────────────────────────

        /// <summary>
        /// Returns all workout templates available to the client:
        /// PREBUILT templates (visible to everyone), TRAINER_ASSIGNED and CUSTOM
        /// templates scoped to this specific client.
        /// Exercises are loaded for each template.
        /// </summary>
        public List<WorkoutTemplate> GetAvailableWorkouts(int clientId)
        {
            const string sql = @"
                SELECT
                    wt.workout_template_id,
                    wt.client_id,
                    wt.name,
                    wt.type
                FROM WORKOUT_TEMPLATE wt
                WHERE wt.type = 'PREBUILT'
                   OR (wt.type = 'TRAINER_ASSIGNED' AND wt.client_id = @ClientId)
                   OR (wt.type = 'CUSTOM'           AND wt.client_id = @ClientId)
                ORDER BY wt.type, wt.name;";

            var templates = new List<WorkoutTemplate>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var template = new WorkoutTemplate
                    {
                        Id = reader.GetInt32(0),
                        ClientId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Type = Enum.Parse<WorkoutType>(reader.GetString(3))
                    };
                    templates.Add(template);
                }
            }

            // Load exercises for each template via AddExercise.
            foreach (var template in templates)
            {
                var exercises = LoadExercisesForTemplate(template.Id, conn);
                foreach (var exercise in exercises)
                {
                    template.AddExercise(exercise);
                }
            }

            return templates;
        }

        // ── TEMPLATE_EXERCISE ────────────────────────────────────────────────

        /// <summary>
        /// Loads a single TemplateExercise by primary key.
        /// Returns null if not found.
        /// </summary>
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

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", templateExerciseId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new TemplateExercise
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                WorkoutTemplateId = reader.GetInt32(2),
                MuscleGroup = Enum.Parse<MuscleGroup>(reader.GetString(3)),
                TargetSets = reader.GetInt32(4),
                TargetReps = reader.GetInt32(5),
                TargetWeight = reader.GetDouble(6)
            };
        }

        /// <summary>
        /// Updates the prescribed weight on a template exercise row.
        /// Called by ProgressionService after a successful overload or deload.
        /// </summary>
        public bool UpdateTemplateWeight(int templateExerciseId, double newWeight)
        {
            const string sql = @"
                UPDATE TEMPLATE_EXERCISE
                SET target_weight = @NewWeight
                WHERE id = @Id;";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@NewWeight", newWeight);
            cmd.Parameters.AddWithValue("@Id", templateExerciseId);

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        // ── Private helper ───────────────────────────────────────────────────

        /// <summary>
        /// Loads all exercises for a given workout template.
        /// Reuses an open connection to avoid re-opening inside a loop.
        /// </summary>
        private List<TemplateExercise> LoadExercisesForTemplate(int templateId, SqlConnection conn)
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

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TemplateId", templateId);

            using var reader = cmd.ExecuteReader();
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
                    TargetWeight = reader.GetDouble(6)
                });
            }

            return exercises;
        }
    }
}
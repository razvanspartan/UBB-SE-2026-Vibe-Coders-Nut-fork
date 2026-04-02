using Microsoft.Data.SqlClient;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage 
    {
        /// <summary>
        /// Persists a completed WorkoutLog with all its exercises and sets.
        /// Uses a transaction so a partial failure rolls back cleanly.
        /// </summary>
        public bool SaveWorkoutLog(WorkoutLog log)
        {
            const string insertLog = @"
                INSERT INTO WORKOUT_LOG 
                    (client_id, workout_id, date, total_duration, calories_burned, rating)
                VALUES 
                    (@ClientId, @WorkoutId, @Date, @Duration, @CaloriesBurned, @Rating);
                SELECT SCOPE_IDENTITY();";

            const string insertSet = @"
                INSERT INTO WORKOUT_LOG_SETS
                    (workout_log_id, exercise_name, sets, reps, weight, 
                     target_reps, target_weight, performance_ratio,
                     is_system_adjusted, adjustment_note)
                VALUES
                    (@WorkoutLogId, @ExerciseName, @SetIndex, @ActualReps, @ActualWeight,
                     @TargetReps, @TargetWeight, @PerformanceRatio,
                     @IsSystemAdjusted, @AdjustmentNote);";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Insert the log header and get the generated id.
                int workoutLogId;
                using (var cmd = new SqlCommand(insertLog, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@ClientId", log.ClientId);
                    cmd.Parameters.AddWithValue("@WorkoutId", log.SourceTemplateId);
                    cmd.Parameters.AddWithValue("@Date", log.Date);
                    cmd.Parameters.AddWithValue("@Duration", log.Duration.ToString());
                    cmd.Parameters.AddWithValue("@CaloriesBurned", log.TotalCaloriesBurned);
                    cmd.Parameters.AddWithValue("@Rating", DBNull.Value);

                    workoutLogId = Convert.ToInt32(cmd.ExecuteScalar());
                    log.Id = workoutLogId;
                }

                // 2. Insert each set for every exercise.
                foreach (var exercise in log.Exercises)
                {
                    foreach (var set in exercise.Sets)
                    {
                        using var cmd = new SqlCommand(insertSet, conn, transaction);
                        cmd.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);
                        cmd.Parameters.AddWithValue("@ExerciseName", exercise.ExerciseName);
                        cmd.Parameters.AddWithValue("@SetIndex", set.SetIndex);
                        cmd.Parameters.AddWithValue("@ActualReps", (object?)set.ActualReps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ActualWeight", (object?)set.ActualWeight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TargetReps", (object?)set.TargetReps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TargetWeight", (object?)set.TargetWeight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PerformanceRatio", exercise.PerformanceRatio);
                        cmd.Parameters.AddWithValue("@IsSystemAdjusted", exercise.IsSystemAdjusted);
                        cmd.Parameters.AddWithValue("@AdjustmentNote", exercise.AdjustmentNote);

                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"SaveWorkoutLog failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns the client's full workout history ordered by date descending.
        /// Exercises and sets are loaded for each log.
        /// </summary>
        public List<WorkoutLog> GetWorkoutHistory(int clientId)
        {
            const string sqlLogs = @"
                SELECT 
                    wl.workout_log_id,
                    wl.date,
                    wl.total_duration,
                    wl.calories_burned,
                    wl.workout_id,
                    wl.rating,
                    wl.trainer_notes,
                    wt.name
                FROM WORKOUT_LOG wl
                LEFT JOIN WORKOUT_TEMPLATE wt ON wl.workout_id = wt.workout_template_id
                WHERE wl.client_id = @ClientId
                ORDER BY wl.date DESC;";

            var logs = new List<WorkoutLog>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new SqlCommand(sqlLogs, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(new WorkoutLog
                    {
                        Id = reader.GetInt32(0),
                        Date = reader.GetDateTime(1),
                        Duration = TimeSpan.Parse(reader.GetString(2)),
                        TotalCaloriesBurned = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        SourceTemplateId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        Rating = reader.IsDBNull(5) ? -1 : Convert.ToDouble(reader.GetInt32(5)),
                        TrainerNotes = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        WorkoutName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        ClientId = clientId
                    });
                }
            }

            // Load sets for each log.
            foreach (var log in logs)
            {
                log.Exercises = LoadExercisesForLog(log.Id, conn);
            }

            return logs;
        }

        /// <summary>
        /// Returns the two most recent WorkoutLogs that contain a set linked
        /// to the given template exercise. Used by ProgressionService for
        /// plateau detection across sessions.
        /// </summary>
        public List<WorkoutLog> GetLastTwoLogsForExercise(int templateExerciseId)
        {
            const string sql = @"
                SELECT TOP 2
                    wl.workout_log_id,
                    wl.client_id,
                    wl.date,
                    wl.total_duration,
                    wl.calories_burned,
                    wl.workout_id
                FROM WORKOUT_LOG wl
                INNER JOIN WORKOUT_LOG_SETS wls ON wls.workout_log_id = wl.workout_log_id
                INNER JOIN TEMPLATE_EXERCISE te  ON te.name = wls.exercise_name
                WHERE te.id = @TemplateExerciseId
                ORDER BY wl.date DESC;";

            var logs = new List<WorkoutLog>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TemplateExerciseId", templateExerciseId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(new WorkoutLog
                    {
                        Id = reader.GetInt32(0),
                        ClientId = reader.GetInt32(1),
                        Date = reader.GetDateTime(2),
                        Duration = TimeSpan.Parse(reader.GetString(3)),
                        TotalCaloriesBurned = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        SourceTemplateId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                    });
                }
            }

            foreach (var log in logs)
            {
                log.Exercises = LoadExercisesForLog(log.Id, conn);
            }

            return logs;
        }


        public bool UpdateWorkoutLogFeedback(int workoutLogId, double rating, string notes)
        {
            const string sql = @"
                UPDATE WORKOUT_LOG 
                SET rating = @Rating, 
                    trainer_notes = @Notes
                WHERE workout_log_id = @WorkoutLogId;";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);

            // If rating is -1 (our default), save it as NULL in the database
            cmd.Parameters.AddWithValue("@Rating", rating == -1 ? DBNull.Value : (int)rating);

            // If notes are empty, save as NULL, otherwise save the text
            cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes);

            try
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update feedback: {ex.Message}");
                return false;
            }
        }


        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Loads all exercises and their sets for a given workout log id.
        /// Reuses an open connection to avoid re-opening inside a loop.
        /// </summary>
        private List<LoggedExercise> LoadExercisesForLog(int workoutLogId, SqlConnection conn)
        {
            const string sql = @"
                SELECT
                    wls.workout_log_sets_id,
                    wls.exercise_name,
                    wls.sets,
                    wls.reps,
                    wls.weight,
                    wls.target_reps,
                    wls.target_weight,
                    wls.performance_ratio,
                    wls.is_system_adjusted,
                    wls.adjustment_note,
                    te.muscle_group
                FROM WORKOUT_LOG_SETS wls
                LEFT JOIN TEMPLATE_EXERCISE te ON wls.exercise_name = te.name
                WHERE wls.workout_log_id = @WorkoutLogId
                ORDER BY wls.exercise_name, wls.sets;";

            var exerciseMap = new Dictionary<string, LoggedExercise>();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);

            using var reader = cmd.ExecuteReader();
            
            while (reader.Read())
            {
                string dbMuscleString = reader.IsDBNull(10) ? "OTHER" : reader.GetString(10);
                string exerciseName = reader.GetString(1);

                Enum.TryParse<MuscleGroup>(dbMuscleString, true, out var parsedMuscleGroup);

                if (!exerciseMap.TryGetValue(exerciseName, out var exercise))
                {
                    exercise = new LoggedExercise
                    {
                        WorkoutLogId = workoutLogId,
                        ExerciseName = exerciseName,
                        PerformanceRatio = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                        IsSystemAdjusted = !reader.IsDBNull(8) && reader.GetBoolean(8),
                        AdjustmentNote = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                        TargetMuscles = parsedMuscleGroup
                    };
                    exerciseMap[exerciseName] = exercise;
                }

                exercise.Sets.Add(new LoggedSet
                {
                    Id = reader.GetInt32(0),
                    WorkoutLogId = workoutLogId,
                    ExerciseName = exerciseName,
                    SetIndex = reader.GetInt32(2),
                    ActualReps = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    ActualWeight = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    TargetReps = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TargetWeight = reader.IsDBNull(6) ? null : reader.GetDouble(6)
                });
            }

            return exerciseMap.Values.ToList();
        }
    }
}
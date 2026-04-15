using Microsoft.Data.Sqlite;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        public double GetClientWeight(int clientId)
        {
            const string sql = "SELECT weight FROM CLIENT WHERE client_id = @ClientId LIMIT 1;";
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            var result = cmd.ExecuteScalar();
            return result is null || result == DBNull.Value ? 75.0 : Convert.ToDouble(result);
        }

        public bool SaveWorkoutLog(WorkoutLog log)
        {
            const string insertLog = @"
                INSERT INTO WORKOUT_LOG
                    (client_id, workout_id, type, date, total_duration, calories_burned, rating, intensity_tag)
                VALUES
                    (@ClientId, @WorkoutId, @Type, @Date, @Duration, @CaloriesBurned, @Rating, @IntensityTag);";

            const string insertSet = @"
                INSERT INTO WORKOUT_LOG_SETS
                    (workout_log_id, exercise_name, sets, reps, weight,
                     target_reps, target_weight, performance_ratio,
                     is_system_adjusted, adjustment_note)
                VALUES
                    (@WorkoutLogId, @ExerciseName, @SetIndex, @ActualReps, @ActualWeight,
                     @TargetReps, @TargetWeight, @PerformanceRatio,
                     @IsSystemAdjusted, @AdjustmentNote);";

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                int workoutLogId;
                using (var cmd = new SqliteCommand(insertLog, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@ClientId",      log.ClientId);
                    cmd.Parameters.AddWithValue("@WorkoutId",     log.SourceTemplateId);
                    cmd.Parameters.AddWithValue("@Type",          SerializeWorkoutType(log.Type));
                    cmd.Parameters.AddWithValue("@Date",          log.Date.ToString("o"));
                    cmd.Parameters.AddWithValue("@Duration",      log.Duration.ToString());
                    cmd.Parameters.AddWithValue("@CaloriesBurned", log.TotalCaloriesBurned);
                    cmd.Parameters.AddWithValue("@Rating",        DBNull.Value);
                    cmd.Parameters.AddWithValue("@IntensityTag",  log.IntensityTag ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }

                using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, transaction))
                {
                    workoutLogId = Convert.ToInt32(idCmd.ExecuteScalar());
                    log.Id = workoutLogId;
                }

                foreach (var exercise in log.Exercises)
                {
                    foreach (var set in exercise.Sets)
                    {
                        using var cmd = new SqliteCommand(insertSet, conn, transaction);
                        cmd.Parameters.AddWithValue("@WorkoutLogId",       workoutLogId);
                        cmd.Parameters.AddWithValue("@ExerciseName",       exercise.ExerciseName);
                        cmd.Parameters.AddWithValue("@SetIndex",           set.SetIndex);
                        cmd.Parameters.AddWithValue("@ActualReps",         (object?)set.ActualReps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ActualWeight",       (object?)set.ActualWeight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TargetReps",         (object?)set.TargetReps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TargetWeight",       (object?)set.TargetWeight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PerformanceRatio",   exercise.PerformanceRatio);
                        cmd.Parameters.AddWithValue("@IsSystemAdjusted",   exercise.IsSystemAdjusted ? 1 : 0);
                        cmd.Parameters.AddWithValue("@AdjustmentNote",     exercise.AdjustmentNote);
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
                    wt.name,
                    wl.type
                FROM WORKOUT_LOG wl
                LEFT JOIN WORKOUT_TEMPLATE wt ON wl.workout_id = wt.workout_template_id
                WHERE wl.client_id = @ClientId
                ORDER BY wl.date DESC;";

            var logs = new List<WorkoutLog>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using (var cmd = new SqliteCommand(sqlLogs, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(new WorkoutLog
                    {
                        Id = reader.GetInt32(0),
                        Date = DateTime.Parse(reader.GetString(1)),
                        Duration = TimeSpan.Parse(reader.GetString(2)),
                        TotalCaloriesBurned = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        SourceTemplateId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        Rating = reader.IsDBNull(5) ? -1 : Convert.ToDouble(reader.GetInt32(5)),
                        TrainerNotes = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        WorkoutName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        Type = ParseWorkoutType(reader.IsDBNull(8) ? null : reader.GetString(8)),
                                        ClientId = clientId
                                    });
                                }
                            }

                            foreach (var log in logs)
                            {
                                log.Exercises = LoadExercisesForLog(log.Id, conn);
                            }

                            return logs;
                        }

                        public bool UpdateWorkoutLog(WorkoutLog log)
        {
            const string updateLog = @"
                UPDATE WORKOUT_LOG
                SET total_duration  = @Duration,
                    calories_burned = @CaloriesBurned,
                    intensity_tag   = @IntensityTag,
                    rating          = @Rating,
                    trainer_notes   = @TrainerNotes
                WHERE workout_log_id = @WorkoutLogId;";

            const string deleteSets = @"
                DELETE FROM WORKOUT_LOG_SETS
                WHERE workout_log_id = @WorkoutLogId;";

            const string insertSet = @"
                INSERT INTO WORKOUT_LOG_SETS
                    (workout_log_id, exercise_name, sets, reps, weight,
                     target_reps, target_weight, performance_ratio,
                     is_system_adjusted, adjustment_note)
                VALUES
                    (@WorkoutLogId, @ExerciseName, @SetIndex, @ActualReps, @ActualWeight,
                     @TargetReps, @TargetWeight, @PerformanceRatio,
                     @IsSystemAdjusted, @AdjustmentNote);";

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqliteCommand(updateLog, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@WorkoutLogId", log.Id);
                    cmd.Parameters.AddWithValue("@Duration", log.Duration.ToString());
                    cmd.Parameters.AddWithValue("@CaloriesBurned", log.TotalCaloriesBurned);
                    cmd.Parameters.AddWithValue("@IntensityTag", log.IntensityTag ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Rating", log.Rating == -1 ? DBNull.Value : (object)(int)log.Rating);
                    cmd.Parameters.AddWithValue("@TrainerNotes", string.IsNullOrWhiteSpace(log.TrainerNotes) ? DBNull.Value : (object)log.TrainerNotes);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqliteCommand(deleteSets, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@WorkoutLogId", log.Id);
                    cmd.ExecuteNonQuery();
                }

                foreach (var exercise in log.Exercises)
                {
                    var orderedSets = exercise.Sets
                        .OrderBy(s => s.SetIndex)
                        .ToList();

                    for (int i = 0; i < orderedSets.Count; i++)
                    {
                        var set = orderedSets[i];
                        using var cmd = new SqliteCommand(insertSet, conn, transaction);
                        cmd.Parameters.AddWithValue("@WorkoutLogId", log.Id);
                        cmd.Parameters.AddWithValue("@ExerciseName", exercise.ExerciseName);
                        cmd.Parameters.AddWithValue("@SetIndex", i + 1);
                        cmd.Parameters.AddWithValue("@ActualReps", (object?)set.ActualReps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ActualWeight", (object?)set.ActualWeight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TargetReps", (object?)set.TargetReps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TargetWeight", (object?)set.TargetWeight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PerformanceRatio", exercise.PerformanceRatio);
                        cmd.Parameters.AddWithValue("@IsSystemAdjusted", exercise.IsSystemAdjusted ? 1 : 0);
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
                System.Diagnostics.Debug.WriteLine($"UpdateWorkoutLog failed: {ex.Message}");
                return false;
            }
        }

        public List<WorkoutLog> GetLastTwoLogsForExercise(int templateExerciseId)
        {
            const string sql = @"
                SELECT
                    wl.workout_log_id,
                    wl.client_id,
                    wl.date,
                    wl.total_duration,
                    wl.calories_burned,
                    wl.workout_id,
                    wl.type
                FROM WORKOUT_LOG wl
                INNER JOIN WORKOUT_LOG_SETS  wls ON wls.workout_log_id = wl.workout_log_id
                INNER JOIN TEMPLATE_EXERCISE te  ON te.name = wls.exercise_name
                WHERE te.id = @TemplateExerciseId
                ORDER BY wl.date DESC
                LIMIT 2;";

            var logs = new List<WorkoutLog>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TemplateExerciseId", templateExerciseId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                            logs.Add(new WorkoutLog
                            {
                                Id = reader.GetInt32(0),
                                ClientId = reader.GetInt32(1),
                                Date = DateTime.Parse(reader.GetString(2)),
                                Duration = TimeSpan.Parse(reader.GetString(3)),
                                TotalCaloriesBurned = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                SourceTemplateId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                Type = ParseWorkoutType(reader.IsDBNull(6) ? null : reader.GetString(6))
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
                SET rating        = @Rating,
                    trainer_notes = @Notes
                WHERE workout_log_id = @WorkoutLogId;";

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);
            cmd.Parameters.AddWithValue("@Rating",       rating == -1 ? DBNull.Value : (object)(int)rating);
            cmd.Parameters.AddWithValue("@Notes",        string.IsNullOrWhiteSpace(notes) ? DBNull.Value : (object)notes);

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update feedback: {ex.Message}");
                return false;
            }
        }

        private List<LoggedExercise> LoadExercisesForLog(int workoutLogId, SqliteConnection conn)
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
                    te.muscle_group,
                    te.id
                FROM WORKOUT_LOG_SETS wls
                LEFT JOIN WORKOUT_LOG wl ON wls.workout_log_id = wl.workout_log_id
                LEFT JOIN TEMPLATE_EXERCISE te ON wls.exercise_name = te.name AND te.workout_template_id = wl.workout_id
                WHERE wls.workout_log_id = @WorkoutLogId
                ORDER BY wls.exercise_name, wls.sets;";

            var exerciseMap = new Dictionary<string, LoggedExercise>();

            using var cmd = new SqliteCommand(sql, conn);
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
                        IsSystemAdjusted = !reader.IsDBNull(8) && reader.GetInt32(8) != 0,
                        AdjustmentNote = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                        TargetMuscles = parsedMuscleGroup,
                        ParentTemplateExerciseId = reader.IsDBNull(11) ? 0 : reader.GetInt32(11)
                    };
                    exerciseMap[exerciseName] = exercise;
                }

                int setIndex = reader.GetInt32(2) + 1;
                exercise.Sets.Add(new LoggedSet
                {
                    Id = reader.GetInt32(0),
                    WorkoutLogId = workoutLogId,
                    ExerciseName = exerciseName,
                    SetIndex = setIndex,
                    SetNumber = setIndex,
                    ActualReps = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    ActualWeight = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    TargetReps = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    TargetWeight = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                    Exercise = exercise
                });
            }

            return exerciseMap.Values.ToList();
        }

        public int GetTotalActiveTimeForClient(int clientId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand(@"
                SELECT COALESCE(SUM(
                    CASE
                        WHEN total_duration IS NOT NULL AND total_duration != ''
                        THEN strftime('%s', total_duration) - strftime('%s', '00:00:00')
                        ELSE 0
                    END), 0)
                FROM WORKOUT_LOG
                WHERE client_id = @ClientId;", conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
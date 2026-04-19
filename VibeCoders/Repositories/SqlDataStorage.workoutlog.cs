namespace VibeCoders.Services
{
    using Microsoft.Data.Sqlite;
    using VibeCoders.Models;

    public partial class SqlDataStorage
    {
        public double GetClientWeight(int clientId)
        {
            const string sql = "SELECT weight FROM CLIENT WHERE client_id = @ClientId LIMIT 1;";
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@ClientId", clientId);
            var result = command.ExecuteScalar();
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

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                int workoutLogId;
                using (var command = new SqliteCommand(insertLog, connection, transaction))
                {
                    command.Parameters.AddWithValue("@ClientId", log.ClientId);
                    command.Parameters.AddWithValue("@WorkoutId", log.SourceTemplateId);
                    command.Parameters.AddWithValue("@Type", SerializeWorkoutType(log.Type));
                    command.Parameters.AddWithValue("@Date", log.Date.ToString("o"));
                    command.Parameters.AddWithValue("@Duration", log.Duration.ToString());
                    command.Parameters.AddWithValue("@CaloriesBurned", log.TotalCaloriesBurned);
                    command.Parameters.AddWithValue("@Rating", DBNull.Value);
                    command.Parameters.AddWithValue("@IntensityTag", log.IntensityTag ?? string.Empty);
                    command.ExecuteNonQuery();
                }

                using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection, transaction))
                {
                    workoutLogId = Convert.ToInt32(idCmd.ExecuteScalar());
                    log.Id = workoutLogId;
                }

                foreach (var exercise in log.Exercises)
                {
                    foreach (var set in exercise.Sets)
                    {
                        using var command = new SqliteCommand(insertSet, connection, transaction);
                        command.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);
                        command.Parameters.AddWithValue("@ExerciseName", exercise.ExerciseName);
                        command.Parameters.AddWithValue("@SetIndex", set.SetIndex);
                        command.Parameters.AddWithValue("@ActualReps", (object?)set.ActualReps ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ActualWeight", (object?)set.ActualWeight ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TargetReps", (object?)set.TargetReps ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TargetWeight", (object?)set.TargetWeight ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PerformanceRatio", exercise.PerformanceRatio);
                        command.Parameters.AddWithValue("@IsSystemAdjusted", exercise.IsSystemAdjusted ? 1 : 0);
                        command.Parameters.AddWithValue("@AdjustmentNote", exercise.AdjustmentNote);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"SaveWorkoutLog failed: {exception.Message}");
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

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using (var command = new SqliteCommand(sqlLogs, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);

                using var reader = command.ExecuteReader();
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
                        ClientId = clientId,
                    });
                }
            }

            foreach (var log in logs)
            {
                log.Exercises = this.LoadExercisesForLog(log.Id, connection);
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

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using (var command = new SqliteCommand(updateLog, connection, transaction))
                {
                    command.Parameters.AddWithValue("@WorkoutLogId", log.Id);
                    command.Parameters.AddWithValue("@Duration", log.Duration.ToString());
                    command.Parameters.AddWithValue("@CaloriesBurned", log.TotalCaloriesBurned);
                    command.Parameters.AddWithValue("@IntensityTag", log.IntensityTag ?? string.Empty);
                    command.Parameters.AddWithValue("@Rating", log.Rating == -1 ? DBNull.Value : (object)(int)log.Rating);
                    command.Parameters.AddWithValue("@TrainerNotes", string.IsNullOrWhiteSpace(log.TrainerNotes) ? DBNull.Value : (object)log.TrainerNotes);
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(deleteSets, connection, transaction))
                {
                    command.Parameters.AddWithValue("@WorkoutLogId", log.Id);
                    command.ExecuteNonQuery();
                }

                foreach (var exercise in log.Exercises)
                {
                    var orderedSets = exercise.Sets
                        .OrderBy(s => s.SetIndex)
                        .ToList();

                    for (int i = 0; i < orderedSets.Count; i++)
                    {
                        var set = orderedSets[i];
                        using var command = new SqliteCommand(insertSet, connection, transaction);
                        command.Parameters.AddWithValue("@WorkoutLogId", log.Id);
                        command.Parameters.AddWithValue("@ExerciseName", exercise.ExerciseName);
                        command.Parameters.AddWithValue("@SetIndex", i + 1);
                        command.Parameters.AddWithValue("@ActualReps", (object?)set.ActualReps ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ActualWeight", (object?)set.ActualWeight ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TargetReps", (object?)set.TargetReps ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TargetWeight", (object?)set.TargetWeight ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PerformanceRatio", exercise.PerformanceRatio);
                        command.Parameters.AddWithValue("@IsSystemAdjusted", exercise.IsSystemAdjusted ? 1 : 0);
                        command.Parameters.AddWithValue("@AdjustmentNote", exercise.AdjustmentNote);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"UpdateWorkoutLog failed: {exception.Message}");
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

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@TemplateExerciseId", templateExerciseId);

                using var reader = command.ExecuteReader();
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
                        Type = ParseWorkoutType(reader.IsDBNull(6) ? null : reader.GetString(6)),
                    });
                }
            }

            foreach (var log in logs)
            {
                log.Exercises = this.LoadExercisesForLog(log.Id, connection);
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

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);
            command.Parameters.AddWithValue("@Rating", rating == -1 ? DBNull.Value : (object)(int)rating);
            command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : (object)notes);

            try
            {
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update feedback: {exception.Message}");
                return false;
            }
        }

        private List<LoggedExercise> LoadExercisesForLog(int workoutLogId, SqliteConnection connection)
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

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@WorkoutLogId", workoutLogId);

            using var reader = command.ExecuteReader();
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
                        ParentTemplateExerciseId = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
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
                    Exercise = exercise,
                });
            }

            return exerciseMap.Values.ToList();
        }

        public int GetTotalActiveTimeForClient(int clientId)
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();
            using var command = new SqliteCommand(
                @"
                SELECT COALESCE(SUM(
                    CASE
                        WHEN total_duration IS NOT NULL AND total_duration != ''
                        THEN strftime('%s', total_duration) - strftime('%s', '00:00:00')
                        ELSE 0
                    END), 0)
                FROM WORKOUT_LOG
                WHERE client_id = @ClientId;", connection);
            command.Parameters.AddWithValue("@ClientId", clientId);
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

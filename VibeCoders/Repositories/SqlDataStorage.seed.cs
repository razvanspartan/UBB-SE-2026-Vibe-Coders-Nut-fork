namespace VibeCoders.Repositories
{
    using Microsoft.Data.Sqlite;
    using VibeCoders.Domain;

    public partial class SqlDataStorage
    {
        public void SeedPrebuiltWorkouts()
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            this.SeedTemplate(connection, "HIIT Fat Burner", new[]
            {
                ("Jumping Jacks",     "LEGS", 3, 20),
                ("Burpees",           "CORE", 3, 15),
                ("Mountain Climbers", "CORE", 3, 20),
            });

            this.SeedTemplate(connection, "Full Body Mass", new[]
            {
                ("Back Squat",   "LEGS",  4, 8),
                ("Bench Press",  "CHEST", 4, 8),
                ("Barbell Rows", "BACK",  4, 8),
            });

            this.SeedTemplate(connection, "Full Body Power", new[]
            {
                ("Deadlift",          "BACK",      4, 5),
                ("Overhead Press",    "SHOULDERS", 4, 5),
                ("Weighted Pull-Ups", "BACK",      4, 5),
            });

            this.SeedTemplate(connection, "Endurance Circuit", new[]
            {
                ("Push-Ups",          "CHEST", 3, 20),
                ("Bodyweight Squats", "LEGS",  3, 25),
                ("Plank",             "CORE",  3, 60),
            });
        }

        private void SeedTemplate(
            SqliteConnection connection,
            string name,
            IEnumerable<(string ExerciseName, string MuscleGroup, int Sets, int Reps)> exercises)
        {
            const string checkSql = @"
                SELECT COUNT(1)
                FROM WORKOUT_TEMPLATE
                WHERE name = @Name AND type = 'PRE_BUILT';";

            using (var checkCmd = new SqliteCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@Name", name);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    using var normalizeCmd = new SqliteCommand(
                        @"
                        UPDATE TEMPLATE_EXERCISE
                        SET target_weight = 0
                        WHERE workout_template_id IN (
                            SELECT workout_template_id
                            FROM WORKOUT_TEMPLATE
                            WHERE name = @Name AND type = 'PRE_BUILT'
                        );", connection);
                    normalizeCmd.Parameters.AddWithValue("@Name", name);
                    normalizeCmd.ExecuteNonQuery();
                    return;
                }
            }

            const string insertTemplate = @"
                INSERT INTO WORKOUT_TEMPLATE (client_id, name, type)
                VALUES (0, @Name, 'PRE_BUILT');";

            int templateId;
            using (var insertCmd = new SqliteCommand(insertTemplate, connection))
            {
                insertCmd.Parameters.AddWithValue("@Name", name);
                insertCmd.ExecuteNonQuery();
            }

            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection))
            {
                templateId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            const string insertExercise = @"
                INSERT INTO TEMPLATE_EXERCISE
                    (workout_template_id, name, muscle_group, target_sets, target_reps)
                VALUES
                    (@TemplateId, @Name, @MuscleGroup, @Sets, @Reps);";

            foreach (var (exerciseName, muscleGroup, sets, reps) in exercises)
            {
                using var exerciseCmd = new SqliteCommand(insertExercise, connection);
                exerciseCmd.Parameters.AddWithValue("@TemplateId", templateId);
                exerciseCmd.Parameters.AddWithValue("@Name", exerciseName);
                exerciseCmd.Parameters.AddWithValue("@MuscleGroup", muscleGroup);
                exerciseCmd.Parameters.AddWithValue("@Sets", sets);
                exerciseCmd.Parameters.AddWithValue("@Reps", reps);
                exerciseCmd.ExecuteNonQuery();
            }
        }

        public void SeedAchievementCatalog()
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using (var check = new SqliteCommand("SELECT COUNT(1) FROM ACHIEVEMENT;", connection))
            {
                if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                {
                    return;
                }
            }

            void Insert(string title, string description, string criteria)
            {
                using var command = new SqliteCommand(
                    "INSERT OR IGNORE INTO ACHIEVEMENT (title, description, criteria) VALUES (@Title, @Description, @Criteria);",
                    connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@Criteria", criteria);
                command.ExecuteNonQuery();
            }

            Insert("First Steps", "Prove that you have what it takes to begin.", "Complete your first workout.");
            Insert("Week Warrior", "Show that you can maintain consistency.", "Log workouts on 5 different days.");
            Insert("Dedicated", "Demonstrate your long-term commitment.", "Reach 50 hours of total active time.");
        }

        public void SeedWorkoutMilestoneAchievements()
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            foreach (var milestone in TotalWorkoutsMilestoneEvaluator.DefaultMilestones)
            {
                const string insertSql = @"
                    INSERT OR IGNORE INTO ACHIEVEMENT (title, description, threshold_workouts)
                    VALUES (@Title, @Description, @Threshold);";

                const string updateSql = @"
                    UPDATE ACHIEVEMENT
                    SET threshold_workouts = @Threshold
                    WHERE title = @Title AND threshold_workouts IS NULL;";

                using (var insertCmd = new SqliteCommand(insertSql, connection))
                {
                    insertCmd.Parameters.AddWithValue("@Title", milestone.title);
                    insertCmd.Parameters.AddWithValue("@Description", milestone.description);
                    insertCmd.Parameters.AddWithValue("@Threshold", milestone.threshold);
                    insertCmd.ExecuteNonQuery();
                }

                using (var updateCmd = new SqliteCommand(updateSql, connection))
                {
                    updateCmd.Parameters.AddWithValue("@Title", milestone.title);
                    updateCmd.Parameters.AddWithValue("@Threshold", milestone.threshold);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }

        public void SeedEvaluationEngineAchievements()
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            void Upsert(string title, string description, string criteria)
            {
                const string insertSql = @"
                    INSERT OR IGNORE INTO ACHIEVEMENT (title, description, criteria)
                    VALUES (@Title, @Description, @Criteria);";

                const string updateSql = @"
                    UPDATE ACHIEVEMENT
                    SET description = @Description, criteria = @Criteria
                    WHERE title = @Title;";

                using (var insertCmd = new SqliteCommand(insertSql, connection))
                {
                    insertCmd.Parameters.AddWithValue("@Title", title);
                    insertCmd.Parameters.AddWithValue("@Description", description);
                    insertCmd.Parameters.AddWithValue("@Criteria", criteria);
                    insertCmd.ExecuteNonQuery();
                }

                using (var updateCmd = new SqliteCommand(updateSql, connection))
                {
                    updateCmd.Parameters.AddWithValue("@Title", title);
                    updateCmd.Parameters.AddWithValue("@Description", description);
                    updateCmd.Parameters.AddWithValue("@Criteria", criteria);
                    updateCmd.ExecuteNonQuery();
                }
            }

            Upsert(
                "Week Warrior",
                "Prove you can train every day for a full week.",
                "Log a workout on 7 consecutive calendar days.");

            Upsert(
                "3-Day Streak",
                "Keep the momentum — three days in a row.",
                "Log a workout on 3 consecutive calendar days.");

            Upsert(
                "Iron Week",
                "Push your weekly limits to the top.",
                "Complete 5 workouts within any rolling 7-day window.");
        }

        public void SeedTestData()
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using (var check = new SqliteCommand("SELECT COUNT(1) FROM \"USER\" WHERE username = 'TestTrainer';", connection))
            {
                if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                {
                    return;
                }
            }

            int trainerUserId;
            using (var command = new SqliteCommand("INSERT INTO \"USER\" (username) VALUES ('TestTrainer');", connection))
            {
                command.ExecuteNonQuery();
            }

            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection))
            {
                trainerUserId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int trainerId;
            using (var command = new SqliteCommand("INSERT INTO TRAINER (user_id) VALUES (@UserId);", connection))
            {
                command.Parameters.AddWithValue("@UserId", trainerUserId);
                command.ExecuteNonQuery();
            }

            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection))
            {
                trainerId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int clientUserId;
            using (var command = new SqliteCommand("INSERT INTO \"USER\" (username) VALUES ('TestClient');", connection))
            {
                command.ExecuteNonQuery();
            }

            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection))
            {
                clientUserId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int clientId;
            using (var command = new SqliteCommand(
                @"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height)
                VALUES (@UserId, @TrainerId, 85.5, 180.0);", connection))
            {
                command.Parameters.AddWithValue("@UserId", clientUserId);
                command.Parameters.AddWithValue("@TrainerId", trainerId);
                command.ExecuteNonQuery();
            }

            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection))
            {
                clientId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int CreateLog(DateTime date, string duration, int cals)
            {
                using var command = new SqliteCommand(
                    @"
                    INSERT INTO WORKOUT_LOG (client_id, date, total_duration, calories_burned, rating)
                    VALUES (@ClientId, @Date, @Duration, @Cals, NULL);", connection);
                command.Parameters.AddWithValue("@ClientId", clientId);
                command.Parameters.AddWithValue("@Date", date.ToString("o"));
                command.Parameters.AddWithValue("@Duration", duration);
                command.Parameters.AddWithValue("@Cals", cals);
                command.ExecuteNonQuery();

                using var idCmd = new SqliteCommand("SELECT last_insert_rowid();", connection);
                return Convert.ToInt32(idCmd.ExecuteScalar());
            }

            void AddSet(int logId, string exName, int setIndex, int reps, double weight)
            {
                using var command = new SqliteCommand(
                    @"
                    INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight)
                    VALUES (@LogId, @Name, @SetIdx, @Reps, @Weight);", connection);
                command.Parameters.AddWithValue("@LogId", logId);
                command.Parameters.AddWithValue("@Name", exName);
                command.Parameters.AddWithValue("@SetIdx", setIndex);
                command.Parameters.AddWithValue("@Reps", reps);
                command.Parameters.AddWithValue("@Weight", weight);
                command.ExecuteNonQuery();
            }

            int log1 = CreateLog(DateTime.Now, "01:15:00", 450);
            AddSet(log1, "Barbell Squat", 1, 10, 100.0);
            AddSet(log1, "Barbell Squat", 2, 8, 105.0);
            AddSet(log1, "Barbell Squat", 3, 6, 110.0);
            AddSet(log1, "Romanian Deadlift", 1, 12, 80.0);
            AddSet(log1, "Romanian Deadlift", 2, 12, 80.0);
            AddSet(log1, "Romanian Deadlift", 3, 10, 85.0);
            AddSet(log1, "Romanian Deadlift", 4, 8, 90.0);
            AddSet(log1, "Calf Raises", 1, 15, 60.0);
            AddSet(log1, "Calf Raises", 2, 15, 60.0);

            int log2 = CreateLog(DateTime.Now.AddDays(-3), "00:55:00", 320);
            AddSet(log2, "Bench Press", 1, 10, 80.0);
            AddSet(log2, "Bench Press", 2, 8, 85.0);
            AddSet(log2, "Bench Press", 3, 8, 85.0);
            AddSet(log2, "Overhead Press", 1, 10, 40.0);
            AddSet(log2, "Overhead Press", 2, 10, 40.0);

            int log3 = CreateLog(DateTime.Now.AddDays(-7), "01:05:00", 400);
            AddSet(log3, "Pull-ups", 1, 12, 0.0);
            AddSet(log3, "Pull-ups", 2, 10, 0.0);
            AddSet(log3, "Pull-ups", 3, 8, 0.0);
            AddSet(log3, "Barbell Row", 1, 10, 60.0);
            AddSet(log3, "Barbell Row", 2, 10, 60.0);
            AddSet(log3, "Barbell Row", 3, 8, 65.0);
        }
    }
}

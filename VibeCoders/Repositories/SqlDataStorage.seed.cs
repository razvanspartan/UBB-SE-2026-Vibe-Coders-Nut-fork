using Microsoft.Data.Sqlite;
using VibeCoders.Domain;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        public void SeedPrebuiltWorkouts()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            SeedTemplate(conn, "HIIT Fat Burner", new[]
            {
                ("Jumping Jacks",     "LEGS", 3, 20, 0.0),
                ("Burpees",           "CORE", 3, 15, 0.0),
                ("Mountain Climbers", "CORE", 3, 20, 0.0)
            });

            SeedTemplate(conn, "Full Body Mass", new[]
            {
                ("Back Squat",   "LEGS",  4, 8, 60.0),
                ("Bench Press",  "CHEST", 4, 8, 60.0),
                ("Barbell Rows", "BACK",  4, 8, 50.0)
            });

            SeedTemplate(conn, "Full Body Power", new[]
            {
                ("Deadlift",          "BACK",      4, 5, 100.0),
                ("Overhead Press",    "SHOULDERS", 4, 5,  40.0),
                ("Weighted Pull-Ups", "BACK",      4, 5,  10.0)
            });

            SeedTemplate(conn, "Endurance Circuit", new[]
            {
                ("Push-Ups",          "CHEST", 3, 20, 0.0),
                ("Bodyweight Squats", "LEGS",  3, 25, 0.0),
                ("Plank",             "CORE",  3, 60, 0.0)
            });
        }

        private void SeedTemplate(
            SqliteConnection conn,
            string name,
            IEnumerable<(string ExerciseName, string MuscleGroup, int Sets, int Reps, double Weight)> exercises)
        {
            const string checkSql = @"
                SELECT COUNT(1)
                FROM WORKOUT_TEMPLATE
                WHERE name = @Name AND type = 'PREBUILT';";

            using (var checkCmd = new SqliteCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Name", name);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) return;
            }

            const string insertTemplate = @"
                INSERT INTO WORKOUT_TEMPLATE (client_id, name, type)
                VALUES (0, @Name, 'PREBUILT');";

            int templateId;
            using (var insertCmd = new SqliteCommand(insertTemplate, conn))
            {
                insertCmd.Parameters.AddWithValue("@Name", name);
                insertCmd.ExecuteNonQuery();
            }

            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn))
            {
                templateId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            const string insertExercise = @"
                INSERT INTO TEMPLATE_EXERCISE
                    (workout_template_id, name, muscle_group, target_sets, target_reps, target_weight)
                VALUES
                    (@TemplateId, @Name, @MuscleGroup, @Sets, @Reps, @Weight);";

            foreach (var (exerciseName, muscleGroup, sets, reps, weight) in exercises)
            {
                using var exerciseCmd = new SqliteCommand(insertExercise, conn);
                exerciseCmd.Parameters.AddWithValue("@TemplateId",   templateId);
                exerciseCmd.Parameters.AddWithValue("@Name",         exerciseName);
                exerciseCmd.Parameters.AddWithValue("@MuscleGroup",  muscleGroup);
                exerciseCmd.Parameters.AddWithValue("@Sets",         sets);
                exerciseCmd.Parameters.AddWithValue("@Reps",         reps);
                exerciseCmd.Parameters.AddWithValue("@Weight",       weight);
                exerciseCmd.ExecuteNonQuery();
            }
        }

        public void SeedAchievementCatalog()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using (var check = new SqliteCommand("SELECT COUNT(1) FROM ACHIEVEMENT;", conn))
            {
                if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
            }

            void Insert(string title, string description, string criteria)
            {
                using var cmd = new SqliteCommand(
                    "INSERT INTO ACHIEVEMENT (title, description, criteria) VALUES (@Title, @Description, @Criteria);",
                    conn);
                cmd.Parameters.AddWithValue("@Title",       title);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.Parameters.AddWithValue("@Criteria",    criteria);
                cmd.ExecuteNonQuery();
            }

            Insert("First Steps",  "Prove that you have what it takes to begin.",   "Complete your first workout.");
            Insert("Week Warrior", "Show that you can maintain consistency.",        "Log workouts on 5 different days.");
            Insert("Dedicated",    "Demonstrate your long-term commitment.",         "Reach 50 hours of total active time.");
        }

        public void SeedWorkoutMilestoneAchievements()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            foreach (var milestone in TotalWorkoutsMilestoneEvaluator.DefaultMilestones)
            {
                const string insertSql = @"
                    INSERT OR IGNORE INTO ACHIEVEMENT (title, description, threshold_workouts)
                    VALUES (@Title, @Description, @Threshold);";

                const string updateSql = @"
                    UPDATE ACHIEVEMENT
                    SET threshold_workouts = @Threshold
                    WHERE title = @Title AND threshold_workouts IS NULL;";

                using (var insertCmd = new SqliteCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@Title",       milestone.Title);
                    insertCmd.Parameters.AddWithValue("@Description", milestone.Description);
                    insertCmd.Parameters.AddWithValue("@Threshold",   milestone.Threshold);
                    insertCmd.ExecuteNonQuery();
                }

                using (var updateCmd = new SqliteCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@Title",     milestone.Title);
                    updateCmd.Parameters.AddWithValue("@Threshold", milestone.Threshold);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }

        public void SeedEvaluationEngineAchievements()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            void Upsert(string title, string description, string criteria)
            {
                const string insertSql = @"
                    INSERT OR IGNORE INTO ACHIEVEMENT (title, description, criteria)
                    VALUES (@Title, @Description, @Criteria);";

                const string updateSql = @"
                    UPDATE ACHIEVEMENT
                    SET description = @Description, criteria = @Criteria
                    WHERE title = @Title;";

                using (var insertCmd = new SqliteCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@Title",       title);
                    insertCmd.Parameters.AddWithValue("@Description", description);
                    insertCmd.Parameters.AddWithValue("@Criteria",    criteria);
                    insertCmd.ExecuteNonQuery();
                }

                using (var updateCmd = new SqliteCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@Title",       title);
                    updateCmd.Parameters.AddWithValue("@Description", description);
                    updateCmd.Parameters.AddWithValue("@Criteria",    criteria);
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
                "Week Champion",
                "Push your weekly limits to the top.",
                "Complete 6 workouts within any rolling 7-day window.");
        }

        public void SeedTestData()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using (var check = new SqliteCommand("SELECT COUNT(1) FROM \"USER\" WHERE username = 'TestTrainer';", conn))
            {
                if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
            }

            int trainerUserId;
            using (var cmd = new SqliteCommand("INSERT INTO \"USER\" (username) VALUES ('TestTrainer');", conn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn))
            {
                trainerUserId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int trainerId;
            using (var cmd = new SqliteCommand("INSERT INTO TRAINER (user_id) VALUES (@UserId);", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", trainerUserId);
                cmd.ExecuteNonQuery();
            }
            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn))
            {
                trainerId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int clientUserId;
            using (var cmd = new SqliteCommand("INSERT INTO \"USER\" (username) VALUES ('TestClient');", conn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn))
            {
                clientUserId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int clientId;
            using (var cmd = new SqliteCommand(@"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height)
                VALUES (@UserId, @TrainerId, 85.5, 180.0);", conn))
            {
                cmd.Parameters.AddWithValue("@UserId",    clientUserId);
                cmd.Parameters.AddWithValue("@TrainerId", trainerId);
                cmd.ExecuteNonQuery();
            }
            using (var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn))
            {
                clientId = Convert.ToInt32(idCmd.ExecuteScalar());
            }

            int CreateLog(DateTime date, string duration, int cals)
            {
                using var cmd = new SqliteCommand(@"
                    INSERT INTO WORKOUT_LOG (client_id, date, total_duration, calories_burned, rating)
                    VALUES (@ClientId, @Date, @Duration, @Cals, NULL);", conn);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@Date",     date.ToString("o"));
                cmd.Parameters.AddWithValue("@Duration", duration);
                cmd.Parameters.AddWithValue("@Cals",     cals);
                cmd.ExecuteNonQuery();

                using var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn);
                return Convert.ToInt32(idCmd.ExecuteScalar());
            }

            void AddSet(int logId, string exName, int setIndex, int reps, double weight)
            {
                using var cmd = new SqliteCommand(@"
                    INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight)
                    VALUES (@LogId, @Name, @SetIdx, @Reps, @Weight);", conn);
                cmd.Parameters.AddWithValue("@LogId",  logId);
                cmd.Parameters.AddWithValue("@Name",   exName);
                cmd.Parameters.AddWithValue("@SetIdx", setIndex);
                cmd.Parameters.AddWithValue("@Reps",   reps);
                cmd.Parameters.AddWithValue("@Weight", weight);
                cmd.ExecuteNonQuery();
            }

            int log1 = CreateLog(DateTime.Now, "01:15:00", 450);
            AddSet(log1, "Barbell Squat",    1, 10, 100.0);
            AddSet(log1, "Barbell Squat",    2,  8, 105.0);
            AddSet(log1, "Barbell Squat",    3,  6, 110.0);
            AddSet(log1, "Romanian Deadlift",1, 12,  80.0);
            AddSet(log1, "Romanian Deadlift",2, 12,  80.0);
            AddSet(log1, "Romanian Deadlift",3, 10,  85.0);
            AddSet(log1, "Romanian Deadlift",4,  8,  90.0);
            AddSet(log1, "Calf Raises",      1, 15,  60.0);
            AddSet(log1, "Calf Raises",      2, 15,  60.0);

            int log2 = CreateLog(DateTime.Now.AddDays(-3), "00:55:00", 320);
            AddSet(log2, "Bench Press",    1, 10, 80.0);
            AddSet(log2, "Bench Press",    2,  8, 85.0);
            AddSet(log2, "Bench Press",    3,  8, 85.0);
            AddSet(log2, "Overhead Press", 1, 10, 40.0);
            AddSet(log2, "Overhead Press", 2, 10, 40.0);

            int log3 = CreateLog(DateTime.Now.AddDays(-7), "01:05:00", 400);
            AddSet(log3, "Pull-ups",    1, 12,  0.0);
            AddSet(log3, "Pull-ups",    2, 10,  0.0);
            AddSet(log3, "Pull-ups",    3,  8,  0.0);
            AddSet(log3, "Barbell Row", 1, 10, 60.0);
            AddSet(log3, "Barbell Row", 2, 10, 60.0);
            AddSet(log3, "Barbell Row", 3,  8, 65.0);
        }
    }
}
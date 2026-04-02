using Microsoft.Data.SqlClient;
using VibeCoders.Domain;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        /// <summary>
        /// Seeds the four built-in workout templates if they have not been
        /// inserted yet. Safe to call at every startup — skips insertion when
        /// a PREBUILT template with the same name already exists.
        /// </summary>
        public void SeedPrebuiltWorkouts()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            SeedTemplate(conn, "HIIT Fat Burner", new[]
            {
                ("Jumping Jacks",    "LEGS",      3, 20, 0.0),
                ("Burpees",          "CORE",      3, 15, 0.0),
                ("Mountain Climbers","CORE",      3, 20, 0.0)
            });

            SeedTemplate(conn, "Full Body Mass", new[]
            {
                ("Back Squat",   "LEGS",      4, 8,  60.0),
                ("Bench Press",  "CHEST",     4, 8,  60.0),
                ("Barbell Rows", "BACK",      4, 8,  50.0)
            });

            SeedTemplate(conn, "Full Body Power", new[]
            {
                ("Deadlift",          "BACK",      4, 5,  100.0),
                ("Overhead Press",    "SHOULDERS", 4, 5,  40.0),
                ("Weighted Pull-Ups", "BACK",      4, 5,  10.0)
            });

            SeedTemplate(conn, "Endurance Circuit", new[]
            {
                ("Push-Ups",           "CHEST", 3, 20, 0.0),
                ("Bodyweight Squats",  "LEGS",  3, 25, 0.0),
                ("Plank",              "CORE",  3, 60, 0.0)
            });
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void SeedTemplate(
            SqlConnection conn,
            string name,
            IEnumerable<(string ExerciseName, string MuscleGroup, int Sets, int Reps, double Weight)> exercises)
        {
            // Check if already seeded.
            const string checkSql = @"
                SELECT COUNT(1)
                FROM WORKOUT_TEMPLATE
                WHERE name = @Name AND type = 'PREBUILT';";

            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Name", name);
                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0) return;
            }

            // Insert the template header.
            const string insertTemplate = @"
                INSERT INTO WORKOUT_TEMPLATE (client_id, name, type)
                VALUES (0, @Name, 'PREBUILT');
                SELECT SCOPE_IDENTITY();";

            int templateId;
            using (var insertCmd = new SqlCommand(insertTemplate, conn))
            {
                insertCmd.Parameters.AddWithValue("@Name", name);
                templateId = Convert.ToInt32(insertCmd.ExecuteScalar());
            }

            // Insert exercises for this template.
            const string insertExercise = @"
                INSERT INTO TEMPLATE_EXERCISE
                    (workout_template_id, name, muscle_group, target_sets, target_reps, target_weight)
                VALUES
                    (@TemplateId, @Name, @MuscleGroup, @Sets, @Reps, @Weight);";

            foreach (var (exerciseName, muscleGroup, sets, reps, weight) in exercises)
            {
                using var exerciseCmd = new SqlCommand(insertExercise, conn);
                exerciseCmd.Parameters.AddWithValue("@TemplateId", templateId);
                exerciseCmd.Parameters.AddWithValue("@Name", exerciseName);
                exerciseCmd.Parameters.AddWithValue("@MuscleGroup", muscleGroup);
                exerciseCmd.Parameters.AddWithValue("@Sets", sets);
                exerciseCmd.Parameters.AddWithValue("@Reps", reps);
                exerciseCmd.Parameters.AddWithValue("@Weight", weight);
                exerciseCmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts baseline achievement definitions when <c>ACHIEVEMENT</c> is empty.
        /// Safe to call on every startup.
        /// </summary>
        public void SeedAchievementCatalog()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var check = new SqlCommand("SELECT COUNT(1) FROM ACHIEVEMENT;", conn))
            {
                var count = (int)check.ExecuteScalar();
                if (count > 0)
                {
                    return;
                }
            }

            void Insert(string title, string description, string criteria)
            {
                using var cmd = new SqlCommand(
                    "INSERT INTO ACHIEVEMENT (title, description, criteria) VALUES (@Title, @Description, @Criteria);",
                    conn);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.Parameters.AddWithValue("@Criteria", criteria);
                cmd.ExecuteNonQuery();
            }

            Insert("First Steps",  "Prove that you have what it takes to begin.",  "Complete your first workout.");
            Insert("Week Warrior", "Show that you can maintain consistency.",       "Log workouts on 5 different days.");
            Insert("Dedicated",    "Demonstrate your long-term commitment.",        "Reach 50 hours of total active time.");
        }

        /// <summary>
        /// Upserts the "Total Workouts" milestone achievements defined by
        /// <see cref="TotalWorkoutsMilestoneEvaluator.DefaultMilestones"/> (#186).
        /// Safe to call on every startup.
        /// </summary>
        public void SeedWorkoutMilestoneAchievements()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            foreach (var milestone in TotalWorkoutsMilestoneEvaluator.DefaultMilestones)
            {
                const string upsertSql = @"
                    IF NOT EXISTS (SELECT 1 FROM ACHIEVEMENT WHERE title = @Title)
                        INSERT INTO ACHIEVEMENT (title, description, threshold_workouts)
                        VALUES (@Title, @Description, @Threshold);
                    ELSE
                        UPDATE ACHIEVEMENT
                        SET threshold_workouts = @Threshold
                        WHERE title = @Title AND threshold_workouts IS NULL;";

                using var cmd = new SqlCommand(upsertSql, conn);
                cmd.Parameters.AddWithValue("@Title", milestone.Title);
                cmd.Parameters.AddWithValue("@Description", milestone.Description);
                cmd.Parameters.AddWithValue("@Threshold", milestone.Threshold);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Upserts the streak and weekly-volume achievements evaluated by
        /// <see cref="EvaluationEngine"/>. Also corrects the "Week Warrior" description
        /// to match the StreakCheck(7) rule registered in the engine.
        /// Safe to call on every startup.
        /// </summary>
        public void SeedEvaluationEngineAchievements()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            void Upsert(string title, string description, string criteria)
            {
                const string sql = @"
                    IF NOT EXISTS (SELECT 1 FROM ACHIEVEMENT WHERE title = @Title)
                        INSERT INTO ACHIEVEMENT (title, description, criteria)
                        VALUES (@Title, @Description, @Criteria);
                    ELSE
                        UPDATE ACHIEVEMENT
                        SET description = @Description, criteria = @Criteria
                        WHERE title = @Title;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Title",       title);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.Parameters.AddWithValue("@Criteria",    criteria);
                cmd.ExecuteNonQuery();
            }

            // Align existing "Week Warrior" with StreakCheck(7) registered in EvaluationEngine.
            Upsert(
                "Week Warrior",
                "Prove you can train every day for a full week.",
                "Log a workout on 7 consecutive calendar days.");

            // New: 3-day streak badge.
            Upsert(
                "3-Day Streak",
                "Keep the momentum — three days in a row.",
                "Log a workout on 3 consecutive calendar days.");

            // New: weekly-volume badge.
            Upsert(
                "Week Champion",
                "Push your weekly limits to the top.",
                "Complete 6 workouts within any rolling 7-day window.");
        }

        /// <summary>
        /// Seeds a demo roster: one trainer, <c>DemoClient</c> (primary app persona), and two extra clients
        /// for the trainer dashboard. Workout history is created for <c>DemoClient</c>; ClientAlpha gets one log.
        /// Idempotent: skips when user <c>DemoClient</c> already exists.
        /// </summary>
        public void SeedTestData()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var check = new SqlCommand(
                "SELECT COUNT(1) FROM [USER] WHERE username = 'DemoClient';", conn))
            {
                if ((int)check.ExecuteScalar() > 0) return;
            }

            int trainerUserId;
            using (var cmd = new SqlCommand(
                "INSERT INTO [USER] (username) VALUES ('TestTrainer'); SELECT SCOPE_IDENTITY();", conn))
            {
                trainerUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int trainerId;
            using (var cmd = new SqlCommand(
                "INSERT INTO TRAINER (user_id) VALUES (@UserId); SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", trainerUserId);
                trainerId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int demoUserId;
            using (var cmd = new SqlCommand(
                "INSERT INTO [USER] (username) VALUES ('DemoClient'); SELECT SCOPE_IDENTITY();", conn))
            {
                demoUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int demoClientId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height)
                VALUES (@UserId, @TrainerId, 85.5, 180.0);
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", demoUserId);
                cmd.Parameters.AddWithValue("@TrainerId", trainerId);
                demoClientId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int alphaUserId;
            using (var cmd = new SqlCommand(
                "INSERT INTO [USER] (username) VALUES ('ClientAlpha'); SELECT SCOPE_IDENTITY();", conn))
            {
                alphaUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int alphaClientId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height)
                VALUES (@UserId, @TrainerId, 72, 175);
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", alphaUserId);
                cmd.Parameters.AddWithValue("@TrainerId", trainerId);
                alphaClientId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int betaUserId;
            using (var cmd = new SqlCommand(
                "INSERT INTO [USER] (username) VALUES ('ClientBeta'); SELECT SCOPE_IDENTITY();", conn))
            {
                betaUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int betaClientId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height)
                VALUES (@UserId, @TrainerId, 68, 165);
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", betaUserId);
                cmd.Parameters.AddWithValue("@TrainerId", trainerId);
                betaClientId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int GetPrebuiltTemplateId(string templateName)
            {
                using var cmd = new SqlCommand(@"
                    SELECT TOP 1 workout_template_id
                    FROM WORKOUT_TEMPLATE
                    WHERE name = @Name AND type = 'PREBUILT';", conn);
                cmd.Parameters.AddWithValue("@Name", templateName);
                var o = cmd.ExecuteScalar();
                return o != null ? Convert.ToInt32(o) : 0;
            }

            int tplMass = GetPrebuiltTemplateId("Full Body Mass");
            int tplHiit = GetPrebuiltTemplateId("HIIT Fat Burner");
            int tplPower = GetPrebuiltTemplateId("Full Body Power");
            int tplEndurance = GetPrebuiltTemplateId("Endurance Circuit");

            int CreateLog(int forClientId, DateTime date, string duration, int cals, int workoutTemplateId)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO WORKOUT_LOG (client_id, workout_id, date, total_duration, calories_burned, rating)
                    VALUES (@ClientId, @WorkoutId, @Date, @Duration, @Cals, 5);
                    SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@ClientId", forClientId);
                cmd.Parameters.AddWithValue("@WorkoutId",
                    workoutTemplateId > 0 ? workoutTemplateId : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Date", date);
                cmd.Parameters.AddWithValue("@Duration", duration);
                cmd.Parameters.AddWithValue("@Cals", cals);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }

            void AddSet(int logId, string exName, int setIndex, int reps, double weight)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight)
                    VALUES (@LogId, @Name, @SetIdx, @Reps, @Weight);", conn);
                cmd.Parameters.AddWithValue("@LogId", logId);
                cmd.Parameters.AddWithValue("@Name", exName);
                cmd.Parameters.AddWithValue("@SetIdx", setIndex);
                cmd.Parameters.AddWithValue("@Reps", reps);
                cmd.Parameters.AddWithValue("@Weight", weight);
                cmd.ExecuteNonQuery();
            }

            var today = DateTime.Today;

            // DemoClient — rich WORKOUT_LOG history (names come from workout_id → WORKOUT_TEMPLATE join).
            // Matches the variety users see on the analytics dashboard (separate table).
            int log1 = CreateLog(demoClientId, today, "01:15:00", 450, tplMass);
            AddSet(log1, "Back Squat", 1, 10, 100.0);
            AddSet(log1, "Back Squat", 2, 8, 105.0);
            AddSet(log1, "Back Squat", 3, 6, 110.0);
            AddSet(log1, "Romanian Deadlift", 1, 12, 80.0);
            AddSet(log1, "Romanian Deadlift", 2, 12, 80.0);
            AddSet(log1, "Romanian Deadlift", 3, 10, 85.0);
            AddSet(log1, "Romanian Deadlift", 4, 8, 90.0);
            AddSet(log1, "Barbell Rows", 1, 10, 60.0);
            AddSet(log1, "Barbell Rows", 2, 10, 60.0);

            int log2 = CreateLog(demoClientId, today.AddDays(-3), "00:55:00", 320, tplMass);
            AddSet(log2, "Bench Press", 1, 10, 80.0);
            AddSet(log2, "Bench Press", 2, 8, 85.0);
            AddSet(log2, "Bench Press", 3, 8, 85.0);
            AddSet(log2, "Barbell Rows", 1, 10, 60.0);
            AddSet(log2, "Barbell Rows", 2, 10, 60.0);

            int log3 = CreateLog(demoClientId, today.AddDays(-7), "01:05:00", 400, tplPower);
            AddSet(log3, "Deadlift", 1, 5, 100.0);
            AddSet(log3, "Overhead Press", 1, 5, 40.0);
            AddSet(log3, "Weighted Pull-Ups", 1, 5, 10.0);

            // Additional sessions on distinct days (trainer list + Week Warrior criteria)
            int log4 = CreateLog(demoClientId, today.AddDays(-1), "00:40:00", 280, tplHiit);
            AddSet(log4, "Jumping Jacks", 1, 20, 0.0);
            AddSet(log4, "Burpees", 1, 15, 0.0);

            int log5 = CreateLog(demoClientId, today.AddDays(-5), "00:50:00", 300, tplEndurance);
            AddSet(log5, "Push-Ups", 1, 20, 0.0);
            AddSet(log5, "Bodyweight Squats", 1, 25, 0.0);

            int log6 = CreateLog(demoClientId, today.AddDays(-10), "01:00:00", 360, tplMass);
            AddSet(log6, "Back Squat", 1, 8, 95.0);
            AddSet(log6, "Bench Press", 1, 8, 75.0);

            int log7 = CreateLog(demoClientId, today.AddDays(-14), "00:45:00", 260, tplHiit);
            AddSet(log7, "Mountain Climbers", 1, 20, 0.0);

            int log8 = CreateLog(demoClientId, today.AddDays(-21), "00:55:00", 310, tplPower);
            AddSet(log8, "Deadlift", 1, 5, 95.0);

            // Extra client — two logs for trainer dashboard
            int logAlpha = CreateLog(alphaClientId, today.AddDays(-2), "00:40:00", 210, tplMass);
            AddSet(logAlpha, "Bench Press", 1, 8, 70.0);
            AddSet(logAlpha, "Bench Press", 2, 8, 70.0);

            int logAlpha2 = CreateLog(alphaClientId, today.AddDays(-9), "00:35:00", 180, tplEndurance);
            AddSet(logAlpha2, "Plank", 1, 60, 0.0);

            int logBeta = CreateLog(betaClientId, today.AddDays(-4), "00:30:00", 150, tplHiit);
            AddSet(logBeta, "Burpees", 1, 12, 0.0);
            AddSet(logBeta, "Jumping Jacks", 1, 20, 0.0);

            void UnlockAchievementIfExists(int clientId, string title)
            {
                using var find = new SqlCommand(
                    "SELECT achievement_id FROM ACHIEVEMENT WHERE title = @T;", conn);
                find.Parameters.AddWithValue("@T", title);
                var o = find.ExecuteScalar();
                if (o == null) return;
                int aid = Convert.ToInt32(o);
                using var ins = new SqlCommand(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM CLIENT_ACHIEVEMENT
                        WHERE client_id = @Cid AND achievement_id = @Aid)
                    INSERT INTO CLIENT_ACHIEVEMENT (client_id, achievement_id, unlocked)
                    VALUES (@Cid, @Aid, 1);", conn);
                ins.Parameters.AddWithValue("@Cid", clientId);
                ins.Parameters.AddWithValue("@Aid", aid);
                ins.ExecuteNonQuery();
            }

            UnlockAchievementIfExists(demoClientId, "First Steps");
            UnlockAchievementIfExists(demoClientId, "Week Warrior");
        }

        /// <summary>
        /// Resolves the seeded <c>DemoClient</c> row. Falls back to the lowest <c>client_id</c> if missing.
        /// </summary>
        public int GetDemoClientId()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"
                SELECT c.client_id
                FROM CLIENT c
                INNER JOIN [USER] u ON u.id = c.user_id
                WHERE u.username = 'DemoClient';", conn);
            var o = cmd.ExecuteScalar();
            if (o != null) return Convert.ToInt32(o);

            using var cmd2 = new SqlCommand(
                "SELECT TOP 1 client_id FROM CLIENT ORDER BY client_id;", conn);
            var o2 = cmd2.ExecuteScalar();
            return o2 != null ? Convert.ToInt32(o2) : 1;
        }
    }
}
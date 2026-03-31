using Microsoft.Data.SqlClient;

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

            void Insert(string title, string description)
            {
                using var cmd = new SqlCommand(
                    "INSERT INTO ACHIEVEMENT (title, description) VALUES (@Title, @Description);",
                    conn);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.ExecuteNonQuery();
            }

            Insert("First Steps", "Complete your first workout.");
            Insert("Week Warrior", "Log workouts on five different days.");
            Insert("Dedicated", "Reach 50 hours of total active time.");
        }


        

        /// <summary>
        /// Seeds dummy users, clients, trainers, and workout logs for testing purposes.
        /// Safe to call on every startup.
        /// </summary>
        public void SeedTestData()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // 1. Check if our test trainer already exists. If yes, bail out.
            using (var check = new SqlCommand("SELECT COUNT(1) FROM [USER] WHERE username = 'TestTrainer';", conn))
            {
                if ((int)check.ExecuteScalar() > 0) return;
            }

            // 2. Insert Trainer User
            int trainerUserId;
            using (var cmd = new SqlCommand("INSERT INTO [USER] (username) VALUES ('TestTrainer'); SELECT SCOPE_IDENTITY();", conn))
            {
                trainerUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 3. Insert Trainer Record
            int trainerId;
            using (var cmd = new SqlCommand("INSERT INTO TRAINER (user_id) VALUES (@UserId); SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", trainerUserId);
                trainerId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 4. Insert Client User
            int clientUserId;
            using (var cmd = new SqlCommand("INSERT INTO [USER] (username) VALUES ('TestClient'); SELECT SCOPE_IDENTITY();", conn))
            {
                clientUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 5. Insert Client Record (linked to the Trainer!)
            int clientId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height) 
                VALUES (@UserId, @TrainerId, 85.5, 180.0); 
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", clientUserId);
                cmd.Parameters.AddWithValue("@TrainerId", trainerId);
                clientId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 6. Insert a dummy Workout Log for the Client
            int workoutLogId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO WORKOUT_LOG (client_id, date, total_duration, calories_burned, rating) 
                VALUES (@ClientId, GETDATE(), '01:15:00', 450, 5);
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                workoutLogId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 7. Insert a dummy Set for that Workout
            using (var cmd = new SqlCommand(@"
                INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight)
                VALUES (@LogId, 'Barbell Squat', 1, 10, 100.0);", conn))
            {
                cmd.Parameters.AddWithValue("@LogId", workoutLogId);
                cmd.ExecuteNonQuery();
            }
        }


    }
}
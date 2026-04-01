using Microsoft.Data.SqlClient;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage : IDataStorage
    {

        private readonly string _connectionString = DatabasePaths.GetSqlServerConnectionString();

        /// <summary>
        /// Creates all tables required by the workout tracking and progression
        /// module if they do not already exist. Call this once at application
        /// startup before any other storage operation.
        /// </summary>
        public void EnsureSchemaCreated()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand();
            cmd.Connection = conn;

            // ── USER ─────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='USER' AND xtype='U')
                CREATE TABLE [USER] (
                    id INT PRIMARY KEY IDENTITY(1,1),
                    username VARCHAR(100) NOT NULL
                    
                );";
            cmd.ExecuteNonQuery();

            // ── TRAINER ─────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TRAINER' AND xtype='U')
                CREATE TABLE TRAINER (
                    trainer_id INT PRIMARY KEY IDENTITY(1,1),
                    user_id INT NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES [USER](id)
                );";
            cmd.ExecuteNonQuery();

            // ── CLIENT ─────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CLIENT' AND xtype='U')
                CREATE TABLE CLIENT (
                    client_id INT PRIMARY KEY IDENTITY(1,1),
                    user_id INT NOT NULL,
                    trainer_id INT NOT NULL,
                    weight FLOAT,
                    height FLOAT,
                    FOREIGN KEY (user_id) REFERENCES [USER](id),
                    FOREIGN KEY (trainer_id) REFERENCES TRAINER(trainer_id)
                );";
            cmd.ExecuteNonQuery();



            // ── WORKOUT_TEMPLATE ─────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WORKOUT_TEMPLATE' AND xtype='U')
                CREATE TABLE WORKOUT_TEMPLATE (
                    workout_template_id INT PRIMARY KEY IDENTITY(1,1),
                    client_id           INT NOT NULL,
                    name                VARCHAR(100) NOT NULL,
                    type                VARCHAR(30) NOT NULL
                );";
            cmd.ExecuteNonQuery();

            // ── TEMPLATE_EXERCISE ────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TEMPLATE_EXERCISE' AND xtype='U')
                CREATE TABLE TEMPLATE_EXERCISE (
                    id                  INT PRIMARY KEY IDENTITY(1,1),
                    workout_template_id INT NOT NULL,
                    name                VARCHAR(100) NOT NULL,
                    muscle_group        VARCHAR(30) NOT NULL,
                    target_sets         INT NOT NULL DEFAULT 3,
                    target_reps         INT NOT NULL DEFAULT 10,
                    target_weight       FLOAT NOT NULL DEFAULT 0,
                    FOREIGN KEY (workout_template_id) REFERENCES WORKOUT_TEMPLATE(workout_template_id)
                );";
            cmd.ExecuteNonQuery();

            // ── WORKOUT_LOG ──────────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WORKOUT_LOG' AND xtype='U')
                CREATE TABLE WORKOUT_LOG (
                    workout_log_id      INT PRIMARY KEY IDENTITY(1,1),
                    client_id           INT NOT NULL,
                    workout_id          INT,
                    date                DATETIME NOT NULL,
                    total_duration      VARCHAR(20),
                    calories_burned     INT,
                    rating              INT,
                    FOREIGN KEY (client_id) REFERENCES CLIENT(client_id),
                    FOREIGN KEY (workout_id) REFERENCES WORKOUT_TEMPLATE(workout_template_id)
                );";
            cmd.ExecuteNonQuery();

            // ── WORKOUT_LOG_SETS ─────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WORKOUT_LOG_SETS' AND xtype='U')
                CREATE TABLE WORKOUT_LOG_SETS (
                    workout_log_sets_id INT PRIMARY KEY IDENTITY(1,1),
                    workout_log_id      INT NOT NULL,
                    exercise_name       VARCHAR(100) NOT NULL,
                    sets                INT NOT NULL,
                    reps                INT,
                    weight              FLOAT,
                    target_reps         INT,
                    target_weight       FLOAT,
                    performance_ratio   FLOAT,
                    is_system_adjusted  BIT NOT NULL DEFAULT 0,
                    adjustment_note     VARCHAR(500),
                    FOREIGN KEY (workout_log_id) REFERENCES WORKOUT_LOG(workout_log_id)
                );";
            cmd.ExecuteNonQuery();

            // ── NOTIFICATION ─────────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NOTIFICATION' AND xtype='U')
                CREATE TABLE NOTIFICATION (
                    id              INT PRIMARY KEY IDENTITY(1,1),
                    client_id       INT NOT NULL,
                    title           VARCHAR(100) NOT NULL,
                    message         VARCHAR(1000) NOT NULL,
                    type            VARCHAR(30) NOT NULL,
                    related_id      INT NOT NULL,
                    date_created    DATETIME NOT NULL,
                    is_read         BIT NOT NULL DEFAULT 0,
                    FOREIGN KEY (client_id) REFERENCES CLIENT(client_id)
                );";
            cmd.ExecuteNonQuery();

            // ── ACHIEVEMENT ───────────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ACHIEVEMENT' AND xtype='U')
                CREATE TABLE ACHIEVEMENT (
                    achievement_id  INT PRIMARY KEY IDENTITY(1,1),
                    title           VARCHAR(100) NOT NULL,
                    description     VARCHAR(250) NOT NULL,
                    criteria        VARCHAR(500) NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();

            // ── ACHIEVEMENT migration: add criteria column if it was created without it ──
            cmd.CommandText = @"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('ACHIEVEMENT') AND name = 'criteria'
                )
                    ALTER TABLE ACHIEVEMENT ADD criteria VARCHAR(500) NOT NULL DEFAULT '';";
            cmd.ExecuteNonQuery();

            // ── CLIENT_ACHIEVEMENT ────────────────────────────────────────────
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CLIENT_ACHIEVEMENT' AND xtype='U')
                CREATE TABLE CLIENT_ACHIEVEMENT (
                    client_id       INT NOT NULL,
                    achievement_id  INT NOT NULL,
                    unlocked        BIT NOT NULL DEFAULT 0,
                    CONSTRAINT PK_CLIENT_ACHIEVEMENT PRIMARY KEY (client_id, achievement_id),
                    FOREIGN KEY (client_id) REFERENCES CLIENT(client_id),
                    FOREIGN KEY (achievement_id) REFERENCES ACHIEVEMENT(achievement_id)
                );";
            cmd.ExecuteNonQuery();

            // ── CLIENT_ACHIEVEMENT migration: ensure PK exists on older databases ──
            // The PRIMARY KEY on (client_id, achievement_id) is the DB-level uniqueness
            // enforcer that prevents duplicate awards even under concurrent operations.
            cmd.CommandText = @"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.key_constraints
                    WHERE name = 'PK_CLIENT_ACHIEVEMENT'
                      AND parent_object_id = OBJECT_ID('CLIENT_ACHIEVEMENT')
                )
                    ALTER TABLE CLIENT_ACHIEVEMENT
                        ADD CONSTRAINT PK_CLIENT_ACHIEVEMENT
                        PRIMARY KEY (client_id, achievement_id);";
            cmd.ExecuteNonQuery();
        }
    }
}
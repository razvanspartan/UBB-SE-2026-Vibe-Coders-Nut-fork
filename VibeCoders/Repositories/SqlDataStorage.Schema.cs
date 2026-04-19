namespace VibeCoders.Services
{
    using System;
    using Microsoft.Data.Sqlite;

    public partial class SqlDataStorage : IDataStorage
    {
        private readonly string connectionString = DatabasePaths.GetConnectionString();

        public void EnsureSchemaCreated()
        {
            string sql = LoadSchemaSql();

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();

            MigrateWorkoutLogTypeColumn(connection);
        }

        private static void MigrateWorkoutLogTypeColumn(SqliteConnection connection)
        {
            const string checkSql = @"
                SELECT 1
                FROM pragma_table_info('WORKOUT_LOG')
                WHERE name = 'type'
                LIMIT 1;";

            using var checkCmd = new SqliteCommand(checkSql, connection);
            var exists = checkCmd.ExecuteScalar() is not null;
            if (exists)
            {
                return;
            }

            using var alterCmd = new SqliteCommand(
                @"
                ALTER TABLE WORKOUT_LOG
                ADD COLUMN type TEXT NOT NULL DEFAULT 'CUSTOM';", connection);
            alterCmd.ExecuteNonQuery();
        }

        private static string LoadSchemaSql()
        {
            return @"
CREATE TABLE IF NOT EXISTS ""USER"" (
    id            INTEGER PRIMARY KEY,
    username      TEXT NOT NULL,
    password_hash TEXT NOT NULL DEFAULT '',
    role          TEXT NOT NULL DEFAULT 'CLIENT'
);

CREATE TABLE IF NOT EXISTS TRAINER (
    trainer_id INTEGER PRIMARY KEY,
    user_id    INTEGER NOT NULL,
    FOREIGN KEY (user_id) REFERENCES ""USER""(id)
);

CREATE TABLE IF NOT EXISTS CLIENT (
    client_id  INTEGER PRIMARY KEY,
    user_id    INTEGER NOT NULL,
    trainer_id INTEGER NOT NULL,
    weight     REAL,
    height     REAL,
    FOREIGN KEY (user_id)    REFERENCES ""USER""(id),
    FOREIGN KEY (trainer_id) REFERENCES TRAINER(trainer_id)
);

CREATE TABLE IF NOT EXISTS EXERCISE (
    exercise_id  INTEGER PRIMARY KEY,
    name         TEXT NOT NULL UNIQUE,
    muscle_group TEXT NOT NULL
);

INSERT OR IGNORE INTO EXERCISE (name, muscle_group) VALUES
    ('Bench Press',           'CHEST'),
    ('Incline Dumbbell Press','CHEST'),
    ('Barbell Squat',         'LEGS'),
    ('Leg Press',             'LEGS'),
    ('Deadlift',              'BACK'),
    ('Pull-Ups',              'BACK'),
    ('Overhead Press',        'SHOULDERS'),
    ('Side Laterals',         'SHOULDERS'),
    ('Bicep Curls',           'ARMS'),
    ('Tricep Pushdowns',      'ARMS');

CREATE TABLE IF NOT EXISTS WORKOUT_TEMPLATE (
    workout_template_id INTEGER PRIMARY KEY,
    client_id           INTEGER NOT NULL,
    name                TEXT NOT NULL,
    type                TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS TEMPLATE_EXERCISE (
    id                  INTEGER PRIMARY KEY,
    workout_template_id INTEGER NOT NULL,
    name                TEXT NOT NULL,
    muscle_group        TEXT NOT NULL,
    target_sets         INTEGER NOT NULL DEFAULT 3,
    target_reps         INTEGER NOT NULL DEFAULT 10,
    target_weight       REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (workout_template_id) REFERENCES WORKOUT_TEMPLATE(workout_template_id)
);

CREATE TABLE IF NOT EXISTS WORKOUT_LOG (
    workout_log_id  INTEGER PRIMARY KEY,
    client_id       INTEGER NOT NULL,
    workout_id      INTEGER,
    date            TEXT NOT NULL,
    total_duration  TEXT,
    type            TEXT NOT NULL,
    calories_burned INTEGER,
    rating          INTEGER,
    trainer_notes   TEXT,
    intensity_tag   TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (client_id)  REFERENCES CLIENT(client_id),
    FOREIGN KEY (workout_id) REFERENCES WORKOUT_TEMPLATE(workout_template_id)
);

CREATE TABLE IF NOT EXISTS WORKOUT_LOG_SETS (
    workout_log_sets_id INTEGER PRIMARY KEY,
    workout_log_id      INTEGER NOT NULL,
    exercise_name       TEXT NOT NULL,
    sets                INTEGER NOT NULL,
    reps                INTEGER,
    weight              REAL,
    target_reps         INTEGER,
    target_weight       REAL,
    performance_ratio   REAL,
    is_system_adjusted  INTEGER NOT NULL DEFAULT 0,
    adjustment_note     TEXT,
    FOREIGN KEY (workout_log_id) REFERENCES WORKOUT_LOG(workout_log_id)
);

CREATE TABLE IF NOT EXISTS NOTIFICATION (
    id           INTEGER PRIMARY KEY,
    client_id    INTEGER NOT NULL,
    title        TEXT NOT NULL,
    message      TEXT NOT NULL,
    type         TEXT NOT NULL,
    related_id   INTEGER NOT NULL,
    date_created TEXT NOT NULL,
    is_read      INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (client_id) REFERENCES CLIENT(client_id)
);

CREATE TABLE IF NOT EXISTS ACHIEVEMENT (
    achievement_id     INTEGER PRIMARY KEY,
    title              TEXT NOT NULL,
    description        TEXT NOT NULL,
    criteria           TEXT NOT NULL DEFAULT '',
    threshold_workouts INTEGER
);

CREATE TABLE IF NOT EXISTS CLIENT_ACHIEVEMENT (
    client_id      INTEGER NOT NULL,
    achievement_id INTEGER NOT NULL,
    unlocked       INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (client_id, achievement_id),
    FOREIGN KEY (client_id)      REFERENCES CLIENT(client_id),
    FOREIGN KEY (achievement_id) REFERENCES ACHIEVEMENT(achievement_id)
);

CREATE TABLE IF NOT EXISTS NUTRITION_PLAN (
    nutrition_plan_id INTEGER PRIMARY KEY,
    start_date        TEXT NOT NULL,
    end_date          TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS MEAL (
    meal_id           INTEGER PRIMARY KEY,
    nutrition_plan_id INTEGER NOT NULL,
    name              TEXT NOT NULL,
    ingredients       TEXT NOT NULL,
    instructions      TEXT NOT NULL,
    FOREIGN KEY (nutrition_plan_id) REFERENCES NUTRITION_PLAN(nutrition_plan_id)
);

CREATE TABLE IF NOT EXISTS CLIENT_NUTRITION_PLAN (
    client_id         INTEGER NOT NULL,
    nutrition_plan_id INTEGER NOT NULL,
    PRIMARY KEY (client_id, nutrition_plan_id),
    FOREIGN KEY (client_id)         REFERENCES CLIENT(client_id),
    FOREIGN KEY (nutrition_plan_id) REFERENCES NUTRITION_PLAN(nutrition_plan_id)
);

CREATE INDEX IF NOT EXISTS ix_workout_log_client_date
    ON WORKOUT_LOG (client_id, date DESC, workout_log_id DESC);

CREATE INDEX IF NOT EXISTS ix_workout_log_sets_log_idx
    ON WORKOUT_LOG_SETS (workout_log_id, sets);
";
        }
    }
}

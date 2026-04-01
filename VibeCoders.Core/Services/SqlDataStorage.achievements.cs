using Microsoft.Data.SqlClient;
using VibeCoders.Models;

namespace VibeCoders.Services;

public partial class SqlDataStorage
{
    /// <inheritdoc />
    public int GetConsecutiveWorkoutDayStreak(int clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // "Island" technique: subtracting a row-number from each date groups
        // consecutive days into the same island. The largest island = longest streak.
        const string sql = @"
            WITH WorkoutDays AS (
                SELECT DISTINCT CAST(date AS DATE) AS workout_date
                FROM   WORKOUT_LOG
                WHERE  client_id = @ClientId
            ),
            Islands AS (
                SELECT workout_date,
                       DATEADD(DAY,
                           -CAST(ROW_NUMBER() OVER (ORDER BY workout_date) AS INT),
                           workout_date) AS grp
                FROM WorkoutDays
            )
            SELECT ISNULL(MAX(cnt), 0)
            FROM (
                SELECT COUNT(*) AS cnt
                FROM   Islands
                GROUP BY grp
            ) t;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return (int)cmd.ExecuteScalar();
    }


    /// <inheritdoc />
    public List<Achievement> GetAllAchievements()
    {
        var list = new List<Achievement>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // Pulls the master catalog without joining CLIENT_ACHIEVEMENT —
        // IsUnlocked is left as false because this query is client-agnostic.
        const string sql = @"
            SELECT
                achievement_id,
                title,
                description,
                ISNULL(criteria, '')   AS criteria,
                threshold_workouts
            FROM ACHIEVEMENT
            ORDER BY achievement_id;";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Achievement
            {
                AchievementId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Criteria = reader.GetString(3),
                ThresholdWorkouts = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsUnlocked = false,
            });
        }

        return list;
    }

    /// <inheritdoc />
    public void EvaluateAndUnlockWorkoutMilestones(int clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // Single statement: insert unlock rows for every milestone achievement
        // whose threshold the client has now reached and that is not already unlocked.
        const string sql = @"
            INSERT INTO CLIENT_ACHIEVEMENT (client_id, achievement_id, unlocked)
            SELECT
                @ClientId,
                a.achievement_id,
                1
            FROM ACHIEVEMENT a
            CROSS JOIN (
                SELECT COUNT(*) AS workout_count
                FROM WORKOUT_LOG
                WHERE client_id = @ClientId
            ) stats
            WHERE a.threshold_workouts IS NOT NULL
              AND stats.workout_count >= a.threshold_workouts
              AND NOT EXISTS (
                    SELECT 1
                    FROM CLIENT_ACHIEVEMENT ca
                    WHERE ca.client_id     = @ClientId
                      AND ca.achievement_id = a.achievement_id
                      AND ca.unlocked       = 1
              );";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public int GetWorkoutsInLastSevenDays(int clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT COUNT(*)
            FROM   WORKOUT_LOG
            WHERE  client_id = @ClientId
              AND  date >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
              AND  date <  DATEADD(DAY,  1, CAST(GETDATE() AS DATE));";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return (int)cmd.ExecuteScalar();
    }

    /// <inheritdoc />
    public List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId)
    {
        var list = new List<AchievementShowcaseItem>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // Drive rows from ACHIEVEMENT so every catalog item appears. LEFT JOIN CLIENT_ACHIEVEMENT
        // supplies unlock state when present; missing join = locked. Unlocked rows sort first.
        const string sql = @"
            SELECT
                a.achievement_id,
                a.title,
                a.description,
                a.criteria,
                CASE WHEN ca.unlocked = 1 THEN 1 ELSE 0 END AS is_unlocked
            FROM ACHIEVEMENT a
            LEFT JOIN CLIENT_ACHIEVEMENT ca
                ON ca.achievement_id = a.achievement_id
               AND ca.client_id = @ClientId
            ORDER BY
                CASE WHEN ISNULL(ca.unlocked, 0) = 1 THEN 0 ELSE 1 END,
                a.achievement_id;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AchievementShowcaseItem
            {
                AchievementId = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Criteria = reader.GetString(3),
                IsUnlocked = reader.GetInt32(4) != 0
            });
        }

        return list;
    }

    /// <inheritdoc />
    
    public int GetWorkoutCount(int clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM WORKOUT_LOG WHERE client_id = @ClientId;", conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return (int)cmd.ExecuteScalar();
    }


    /// <inheritdoc />
    public int GetDistinctWorkoutDayCount(int clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(DISTINCT CAST(date AS DATE)) FROM WORKOUT_LOG WHERE client_id = @ClientId;", conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return (int)cmd.ExecuteScalar();
    }

    /// <inheritdoc />
    public AchievementShowcaseItem? GetAchievementForClient(int achievementId, int clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT
                a.achievement_id,
                a.title,
                a.description,
                a.criteria,
                CASE WHEN ca.unlocked = 1 THEN 1 ELSE 0 END AS is_unlocked
            FROM ACHIEVEMENT a
            LEFT JOIN CLIENT_ACHIEVEMENT ca
                ON ca.achievement_id = a.achievement_id
               AND ca.client_id = @ClientId
            WHERE a.achievement_id = @AchievementId;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@AchievementId", achievementId);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new AchievementShowcaseItem
        {
            AchievementId = reader.GetInt32(0),
            Title = reader.GetString(1),
            Description = reader.GetString(2),
            Criteria = reader.GetString(3),
            IsUnlocked = reader.GetInt32(4) != 0
        };
    }

    /// <inheritdoc />
    public bool AwardAchievement(int clientId, int achievementId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // Strict duplicate check: if the client already holds this badge, do nothing.
        const string checkSql = @"
            SELECT COUNT(1)
            FROM CLIENT_ACHIEVEMENT
            WHERE client_id      = @ClientId
              AND achievement_id = @AchievementId
              AND unlocked       = 1;";

        using (var checkCmd = new SqlCommand(checkSql, conn))
        {
            checkCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkCmd.Parameters.AddWithValue("@AchievementId", achievementId);

            var alreadyAwarded = (int)checkCmd.ExecuteScalar() > 0;
            if (alreadyAwarded)
                return false;
        }

        // No existing row — insert with unlocked = 1.
        // Uses INSERT OR UPDATE pattern so a pre-existing locked placeholder is
        // also handled gracefully (e.g. seeded rows with unlocked = 0).
        const string upsertSql = @"
            IF EXISTS (
                SELECT 1 FROM CLIENT_ACHIEVEMENT
                WHERE client_id = @ClientId AND achievement_id = @AchievementId
            )
                UPDATE CLIENT_ACHIEVEMENT
                   SET unlocked = 1
                 WHERE client_id = @ClientId AND achievement_id = @AchievementId;
            ELSE
                INSERT INTO CLIENT_ACHIEVEMENT (client_id, achievement_id, unlocked)
                VALUES (@ClientId, @AchievementId, 1);";

        try
        {
            using var upsertCmd = new SqlCommand(upsertSql, conn);
            upsertCmd.Parameters.AddWithValue("@ClientId", clientId);
            upsertCmd.Parameters.AddWithValue("@AchievementId", achievementId);
            upsertCmd.ExecuteNonQuery();
            return true;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            // PK or unique-index violation — another concurrent request already
            // awarded this badge between our check and the insert. Treat as no-op.
            return false;
        }
    }
}
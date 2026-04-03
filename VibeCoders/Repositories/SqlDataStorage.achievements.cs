using Microsoft.Data.Sqlite;
using VibeCoders.Models;

namespace VibeCoders.Services;

public partial class SqlDataStorage
{
    public int GetConsecutiveWorkoutDayStreak(int clientId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT DISTINCT date(date)
            FROM   WORKOUT_LOG
            WHERE  client_id = @ClientId
            ORDER BY date(date) DESC;";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        
        var dates = new List<DateTime>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (DateTime.TryParse(reader.GetString(0), out var parsedDate))
            {
                dates.Add(parsedDate.Date);
            }
        }

        if (dates.Count == 0) return 0;

        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i - 1] - dates[i]).TotalDays == 1)
            {
                currentStreak++;
                if (currentStreak > maxStreak)
                {
                    maxStreak = currentStreak;
                }
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }

    public List<Achievement> GetAllAchievements()
    {
        var list = new List<Achievement>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT
                achievement_id,
                title,
                description,
                COALESCE(criteria, '') AS criteria,
                threshold_workouts
            FROM ACHIEVEMENT
            ORDER BY achievement_id;";

        using var cmd    = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Achievement
            {
                AchievementId      = reader.GetInt32(0),
                Name               = reader.GetString(1),
                Description        = reader.GetString(2),
                Criteria           = reader.GetString(3),
                ThresholdWorkouts  = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsUnlocked         = false
            });
        }

        return list;
    }

    public void EvaluateAndUnlockWorkoutMilestones(int clientId)
    {
        var achievements = GetAllAchievements();
        int totalWorkouts = GetWorkoutCount(clientId);

        foreach (var achievement in achievements)
        {
            if (achievement.ThresholdWorkouts.HasValue && totalWorkouts >= achievement.ThresholdWorkouts.Value)
            {
                AwardAchievement(clientId, achievement.AchievementId);
            }
        }
    }

    public int GetWorkoutsInLastSevenDays(int clientId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT COUNT(*)
            FROM   WORKOUT_LOG
            WHERE  client_id = @ClientId
              AND  date(date) >= date('now', '-6 days')
              AND  date(date) <= date('now');";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId)
    {
        var list = new List<AchievementShowcaseItem>();

        using var conn = new SqliteConnection(_connectionString);
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
            ORDER BY
                CASE WHEN COALESCE(ca.unlocked, 0) = 1 THEN 0 ELSE 1 END,
                a.achievement_id;";

        using var cmd    = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AchievementShowcaseItem
            {
                AchievementId = reader.GetInt32(0),
                Title         = reader.GetString(1),
                Description   = reader.GetString(2),
                Criteria      = reader.GetString(3),
                IsUnlocked    = reader.GetInt32(4) != 0
            });
        }

        return list;
    }

    public int GetWorkoutCount(int clientId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT COUNT(1) FROM WORKOUT_LOG WHERE client_id = @ClientId;", conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int GetDistinctWorkoutDayCount(int clientId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "SELECT COUNT(DISTINCT date(date)) FROM WORKOUT_LOG WHERE client_id = @ClientId;", conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public AchievementShowcaseItem? GetAchievementForClient(int achievementId, int clientId)
    {
        using var conn = new SqliteConnection(_connectionString);
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

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@AchievementId", achievementId);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new AchievementShowcaseItem
        {
            AchievementId = reader.GetInt32(0),
            Title         = reader.GetString(1),
            Description   = reader.GetString(2),
            Criteria      = reader.GetString(3),
            IsUnlocked    = reader.GetInt32(4) != 0
        };
    }

    public bool AwardAchievement(int clientId, int achievementId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string checkSql = @"
            SELECT COUNT(1)
            FROM CLIENT_ACHIEVEMENT
            WHERE client_id      = @ClientId
              AND achievement_id = @AchievementId
              AND unlocked       = 1;";

        using (var checkCmd = new SqliteCommand(checkSql, conn))
        {
            checkCmd.Parameters.AddWithValue("@ClientId", clientId);
            checkCmd.Parameters.AddWithValue("@AchievementId", achievementId);

            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                return false;
        }

        const string insertSql = @"
            INSERT OR IGNORE INTO CLIENT_ACHIEVEMENT (client_id, achievement_id, unlocked)
            VALUES (@ClientId, @AchievementId, 1);";

        const string updateSql = @"
            UPDATE CLIENT_ACHIEVEMENT
               SET unlocked = 1
             WHERE client_id      = @ClientId
               AND achievement_id = @AchievementId;";

        try
        {
            using (var insertCmd = new SqliteCommand(insertSql, conn))
            {
                insertCmd.Parameters.AddWithValue("@ClientId", clientId);
                insertCmd.Parameters.AddWithValue("@AchievementId", achievementId);
                insertCmd.ExecuteNonQuery();
            }

            using (var updateCmd = new SqliteCommand(updateSql, conn))
            {
                updateCmd.Parameters.AddWithValue("@ClientId", clientId);
                updateCmd.Parameters.AddWithValue("@AchievementId", achievementId);
                updateCmd.ExecuteNonQuery();
            }

            return true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return false;
        }
    }
}
using Microsoft.Data.SqlClient;

namespace VibeCoders.Services;

public partial class SqlDataStorage
{
    /// <summary>
    /// Inserts sample rows into <c>analytics_workout_log</c> for charts/rank when the table is empty.
    /// Uses the same numeric scope as <c>CLIENT.client_id</c> for demo sessions.
    /// </summary>
    public void SeedAnalyticsDemoDataIfEmpty(long clientId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using (var check = new SqlCommand(
            "SELECT COUNT(1) FROM analytics_workout_log WHERE user_id = @uid;", conn))
        {
            check.Parameters.AddWithValue("@uid", clientId);
            if (Convert.ToInt64(check.ExecuteScalar() ?? 0L) > 0)
            {
                return;
            }
        }

        void InsertRow(DateTime date, string name, int durationSeconds, int calories, string intensity)
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO analytics_workout_log
                    (user_id, workout_name, log_date, duration_seconds, source_template_id, total_calories_burned, intensity_tag)
                VALUES
                    (@uid, @name, @date, @dur, 0, @cal, @intensity);", conn);
            cmd.Parameters.AddWithValue("@uid", clientId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@date", date.Date);
            cmd.Parameters.AddWithValue("@dur", durationSeconds);
            cmd.Parameters.AddWithValue("@cal", calories);
            cmd.Parameters.AddWithValue("@intensity", intensity);
            cmd.ExecuteNonQuery();
        }

        var today = DateTime.Today;

        // Recent week — dashboard KPIs and 7-day active time
        InsertRow(today, "Full Body Mass", 3600, 420, "moderate");
        InsertRow(today.AddDays(-1), "HIIT Fat Burner", 2400, 310, "intense");
        InsertRow(today.AddDays(-2), "Full Body Power", 4200, 480, "moderate");
        InsertRow(today.AddDays(-4), "Endurance Circuit", 2700, 290, "light");

        // Spread across prior weeks — consistency chart (4 buckets) + history pages
        for (int w = 1; w <= 5; w++)
        {
            var weekAnchor = today.AddDays(-7 * w);
            InsertRow(weekAnchor, "Full Body Mass", 3300 + w * 120, 380 + w * 5, "moderate");
            InsertRow(weekAnchor.AddDays(2), "HIIT Fat Burner", 2100, 300, "intense");
            InsertRow(weekAnchor.AddDays(4), "Endurance Circuit", 1950, 260, "light");
        }

        // Push lifetime active time toward higher rank / "Dedicated"-style totals (sum of seconds)
        InsertRow(today.AddDays(-120), "Long Run", 20000, 900, "light");
        InsertRow(today.AddDays(-90), "Long Run", 25000, 1100, "light");
        InsertRow(today.AddDays(-60), "Long Run", 30000, 1200, "moderate");
    }
}

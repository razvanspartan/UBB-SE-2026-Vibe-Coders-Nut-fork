using Microsoft.Data.Sqlite;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        public bool SaveNotification(Notification notification)
        {
            const string sql = @"
                INSERT INTO NOTIFICATION
                    (title, message, type, related_id, date_created, is_read, client_id)
                VALUES
                    (@Title, @Message, @Type, @RelatedId, @DateCreated, @IsRead, @ClientId);";

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Title",       notification.Title);
            cmd.Parameters.AddWithValue("@Message",     notification.Message);
            cmd.Parameters.AddWithValue("@Type",        notification.Type.ToString());
            cmd.Parameters.AddWithValue("@RelatedId",   notification.RelatedId);
            cmd.Parameters.AddWithValue("@DateCreated", notification.DateCreated.ToString("o"));
            cmd.Parameters.AddWithValue("@IsRead",      notification.IsRead ? 1 : 0);
            cmd.Parameters.AddWithValue("@ClientId",    notification.ClientId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public List<Notification> GetNotifications(int clientId)
        {
            const string sql = @"
                SELECT
                    id,
                    title,
                    message,
                    type,
                    related_id,
                    date_created,
                    is_read
                FROM NOTIFICATION
                WHERE client_id = @ClientId
                ORDER BY date_created DESC;";

            var notifications = new List<Notification>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd    = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(new Notification
                {
                    Id          = reader.GetInt32(0),
                    Title       = reader.GetString(1),
                    Message     = reader.GetString(2),
                    Type        = Enum.Parse<NotificationType>(reader.GetString(3)),
                    RelatedId   = reader.GetInt32(4),
                    DateCreated = DateTime.Parse(reader.GetString(5)),
                    IsRead      = reader.GetInt32(6) != 0,
                    ClientId    = clientId
                });
            }

            return notifications;
        }
    }
}
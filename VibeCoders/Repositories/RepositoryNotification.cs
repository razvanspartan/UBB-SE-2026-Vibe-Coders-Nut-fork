namespace VibeCoders.Repositories
{
    using Microsoft.Data.Sqlite;
    using VibeCoders.Models;
    using VibeCoders.Repositories.Interfaces;

    public class RepositoryNotification : IRepositoryNotification
    {
        private readonly string connectionString;
        public RepositoryNotification(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public bool SaveNotification(Notification notification)
        {
            const string sql = @"
                INSERT INTO NOTIFICATION
                    (title, message, type, related_id, date_created, is_read, client_id)
                VALUES
                    (@Title, @Message, @Type, @RelatedId, @DateCreated, @IsRead, @ClientId);";

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Title", notification.Title);
            command.Parameters.AddWithValue("@Message", notification.Message);
            command.Parameters.AddWithValue("@Type", notification.Type.ToString());
            command.Parameters.AddWithValue("@RelatedId", notification.RelatedId);
            command.Parameters.AddWithValue("@DateCreated", notification.DateCreated.ToString("o"));
            command.Parameters.AddWithValue("@IsRead", notification.IsRead ? 1 : 0);
            command.Parameters.AddWithValue("@ClientId", notification.ClientId);

            return command.ExecuteNonQuery() > 0;
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

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@ClientId", clientId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(new Notification
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Message = reader.GetString(2),
                    Type = Enum.Parse<NotificationType>(reader.GetString(3)),
                    RelatedId = reader.GetInt32(4),
                    DateCreated = DateTime.Parse(reader.GetString(5)),
                    IsRead = reader.GetInt32(6) != 0,
                    ClientId = clientId,
                });
            }

            return notifications;
        }
    }
}

using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryNotificationTests : IDisposable
{
    private const int FirstTestClientId = 1;
    private const int SecondTestClientId = 2;
    private const int ThirdTestClientId = 3;
    private const int UserIdOffset = 1000;
    private const int DefaultTrainerId = 1;
    private const double DefaultWeight = 75.0;
    private const double DefaultHeight = 180.0;
    private const int DefaultRelatedId = 1;
    private const int TestRelatedId = 42;
    private const int SqliteFalse = 0;
    private const int SqliteTrue = 1;
    private const int FirstNotificationCount = 5;
    private const int SecondNotificationCount = 3;
    private const int TotalNotificationCount = 8;
    private const int OneSecondTolerance = 1;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryNotification repository;

    public RepositoryNotificationTests()
    {
        this.connectionString = "Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared";
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        CreateSchema(this.connection);

        this.repository = new RepositoryNotification(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var command = new SqliteCommand(
            @"
            CREATE TABLE IF NOT EXISTS CLIENT (
                client_id  INTEGER PRIMARY KEY,
                user_id    INTEGER NOT NULL,
                trainer_id INTEGER NOT NULL,
                weight     REAL,
                height     REAL
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
            );", connection);
        command.ExecuteNonQuery();
    }

    [Fact]
    public void SaveNotification_ShouldInsertNotificationIntoDatabase()
    {
        InsertTestClient(FirstTestClientId);
        var notification = CreateTestNotification(FirstTestClientId);

        this.repository.SaveNotification(notification);

        var count = GetNotificationCount(FirstTestClientId);
        count.Should().Be(FirstTestClientId);
    }

    [Fact]
    public void SaveNotification_ShouldSaveAllNotificationProperties()
    {
        InsertTestClient(FirstTestClientId);
        var notification = new Notification
        {
            ClientId = FirstTestClientId,
            Title = "Test Title",
            Message = "Test Message",
            Type = NotificationType.Warning,
            RelatedId = TestRelatedId,
            DateCreated = DateTime.Parse("2024-01-15T10:30:00"),
            IsRead = false
        };

        this.repository.SaveNotification(notification);

        var saved = GetNotificationFromDatabase(FirstTestClientId);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Title");
        saved.Message.Should().Be("Test Message");
        saved.Type.Should().Be("Warning");
        saved.RelatedId.Should().Be(TestRelatedId);
        saved.IsRead.Should().BeFalse();
    }

    [Fact]
    public void SaveNotification_ShouldHandleSpecialCharactersInTitleAndMessage()
    {
        InsertTestClient(FirstTestClientId);
        var notification = CreateTestNotification(FirstTestClientId);
        notification.Title = "Test's \"Title\" with <special> & chars";
        notification.Message = "Message with 'quotes' and \"double quotes\" & symbols";

        this.repository.SaveNotification(notification);

        var saved = GetNotificationFromDatabase(FirstTestClientId);
        saved!.Title.Should().Be("Test's \"Title\" with <special> & chars");
        saved.Message.Should().Be("Message with 'quotes' and \"double quotes\" & symbols");
    }

    [Fact]
    public void GetNotifications_ShouldReturnEmptyList_WhenNoNotificationsExist()
    {
        InsertTestClient(FirstTestClientId);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().BeEmpty();
    }

    [Fact]
    public void GetNotifications_ShouldReturnAllNotificationsForClient()
    {
        InsertTestClient(FirstTestClientId);
        InsertNotification(FirstTestClientId, "Title 1", "Message 1", "Info", FirstTestClientId, DateTime.UtcNow, false);
        InsertNotification(FirstTestClientId, "Title 2", "Message 2", "Warning", SecondTestClientId, DateTime.UtcNow, false);
        InsertNotification(FirstTestClientId, "Title 3", "Message 3", "Plateau", ThirdTestClientId, DateTime.UtcNow, true);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().HaveCount(ThirdTestClientId);
    }

    [Fact]
    public void GetNotifications_ShouldReturnNotificationsOnlyForSpecificClient()
    {
        InsertTestClient(FirstTestClientId);
        InsertTestClient(SecondTestClientId);
        InsertNotification(FirstTestClientId, "Client 1 Notification", "Message", "Info", FirstTestClientId, DateTime.UtcNow, false);
        InsertNotification(SecondTestClientId, "Client 2 Notification", "Message", "Info", SecondTestClientId, DateTime.UtcNow, false);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().HaveCount(FirstTestClientId);
        notifications[SqliteFalse].Title.Should().Be("Client 1 Notification");
        notifications[SqliteFalse].ClientId.Should().Be(FirstTestClientId);
    }

    [Fact]
    public void GetNotifications_ShouldOrderByDateDescending()
    {
        InsertTestClient(FirstTestClientId);
        var oldDate = DateTime.Parse("2024-01-01T10:00:00");
        var middleDate = DateTime.Parse("2024-01-15T10:00:00");
        var recentDate = DateTime.Parse("2024-01-30T10:00:00");

        InsertNotification(FirstTestClientId, "Old", "Message", "Info", FirstTestClientId, oldDate, false);
        InsertNotification(FirstTestClientId, "Recent", "Message", "Info", SecondTestClientId, recentDate, false);
        InsertNotification(FirstTestClientId, "Middle", "Message", "Info", ThirdTestClientId, middleDate, false);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().HaveCount(ThirdTestClientId);
        notifications[SqliteFalse].Title.Should().Be("Recent");
        notifications[FirstTestClientId].Title.Should().Be("Middle");
        notifications[SecondTestClientId].Title.Should().Be("Old");
    }

    [Fact]
    public void GetNotifications_ShouldMapAllPropertiesCorrectly()
    {
        InsertTestClient(FirstTestClientId);
        var date = DateTime.Parse("2024-01-15T10:30:45");
        InsertNotification(FirstTestClientId, "Test Title", "Test Message", "Warning", TestRelatedId, date, true);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().HaveCount(FirstTestClientId);
        var notification = notifications[SqliteFalse];
        notification.Id.Should().BeGreaterThan(SqliteFalse);
        notification.ClientId.Should().Be(FirstTestClientId);
        notification.Title.Should().Be("Test Title");
        notification.Message.Should().Be("Test Message");
        notification.Type.Should().Be(NotificationType.Warning);
        notification.RelatedId.Should().Be(TestRelatedId);
        notification.DateCreated.Should().BeCloseTo(date, TimeSpan.FromSeconds(OneSecondTolerance));
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void IntegrationTest_SaveAndRetrieveMultipleNotifications()
    {
        InsertTestClient(FirstTestClientId);
        InsertTestClient(SecondTestClientId);

        var notification1 = new Notification("Title 1", "Message 1", NotificationType.Info, FirstTestClientId) { ClientId = FirstTestClientId };
        var notification2 = new Notification("Title 2", "Message 2", NotificationType.Warning, SecondTestClientId) { ClientId = FirstTestClientId, IsRead = true };
        var notification3 = new Notification("Title 3", "Message 3", NotificationType.Plateau, ThirdTestClientId) { ClientId = SecondTestClientId };

        this.repository.SaveNotification(notification1);
        this.repository.SaveNotification(notification2);
        this.repository.SaveNotification(notification3);

        var client1Notifications = this.repository.GetNotifications(FirstTestClientId);
        var client2Notifications = this.repository.GetNotifications(SecondTestClientId);

        client1Notifications.Should().HaveCount(SecondTestClientId);
        client2Notifications.Should().HaveCount(FirstTestClientId);

        client1Notifications[SqliteFalse].IsRead.Should().BeTrue();
        client1Notifications[FirstTestClientId].IsRead.Should().BeFalse();
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainDataIntegrity()
    {
        InsertTestClient(FirstTestClientId);
        InsertTestClient(SecondTestClientId);

        for (int index = SqliteFalse; index < FirstNotificationCount; index++)
        {
            var notification = CreateTestNotification(FirstTestClientId, $"Notification {index}");
            this.repository.SaveNotification(notification);
        }

        for (int index = SqliteFalse; index < SecondNotificationCount; index++)
        {
            var notification = CreateTestNotification(SecondTestClientId, $"Notification {index}");
            this.repository.SaveNotification(notification);
        }

        var client1Count = this.repository.GetNotifications(FirstTestClientId).Count;
        var client2Count = this.repository.GetNotifications(SecondTestClientId).Count;
        var totalCount = GetTotalNotificationCount();

        client1Count.Should().Be(FirstNotificationCount);
        client2Count.Should().Be(SecondNotificationCount);
        totalCount.Should().Be(TotalNotificationCount);
    }

    private void InsertTestClient(int clientId)
    {
        using var command = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@clientId, @userId, @trainerId, @weight, @height)",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@userId", clientId + UserIdOffset);
        command.Parameters.AddWithValue("@trainerId", DefaultTrainerId);
        command.Parameters.AddWithValue("@weight", DefaultWeight);
        command.Parameters.AddWithValue("@height", DefaultHeight);
        command.ExecuteNonQuery();
    }

    private void InsertNotification(int clientId, string title, string message, string type, int relatedId, DateTime dateCreated, bool isRead)
    {
        using var command = new SqliteCommand(
            @"INSERT INTO NOTIFICATION (client_id, title, message, type, related_id, date_created, is_read)
              VALUES (@clientId, @title, @message, @type, @relatedId, @dateCreated, @isRead)",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@message", message);
        command.Parameters.AddWithValue("@type", type);
        command.Parameters.AddWithValue("@relatedId", relatedId);
        command.Parameters.AddWithValue("@dateCreated", dateCreated.ToString("o"));
        command.Parameters.AddWithValue("@isRead", isRead ? SqliteTrue : SqliteFalse);
        command.ExecuteNonQuery();
    }

    private Notification CreateTestNotification(int clientId, string titleSuffix = "")
    {
        return new Notification
        {
            ClientId = clientId,
            Title = string.IsNullOrEmpty(titleSuffix) ? "Test Notification" : $"Test Notification {titleSuffix}",
            Message = "Test message content",
            Type = NotificationType.Info,
            RelatedId = DefaultRelatedId,
            DateCreated = DateTime.UtcNow,
            IsRead = false
        };
    }

    private int GetNotificationCount(int clientId)
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM NOTIFICATION WHERE client_id = @clientId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private int GetTotalNotificationCount()
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM NOTIFICATION",
            this.connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private NotificationData? GetNotificationFromDatabase(int clientId)
    {
        using var command = new SqliteCommand(
            @"SELECT title, message, type, related_id, is_read
              FROM NOTIFICATION 
              WHERE client_id = @clientId 
              ORDER BY id DESC 
              LIMIT 1",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        const int titleColumnIndex = 0;
        const int messageColumnIndex = 1;
        const int typeColumnIndex = 2;
        const int relatedIdColumnIndex = 3;
        const int isReadColumnIndex = 4;

        return new NotificationData
        {
            Title = reader.GetString(titleColumnIndex),
            Message = reader.GetString(messageColumnIndex),
            Type = reader.GetString(typeColumnIndex),
            RelatedId = reader.GetInt32(relatedIdColumnIndex),
            IsRead = reader.GetInt32(isReadColumnIndex) != SqliteFalse
        };
    }

    private class NotificationData
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int RelatedId { get; set; }
        public bool IsRead { get; set; }
    }
}

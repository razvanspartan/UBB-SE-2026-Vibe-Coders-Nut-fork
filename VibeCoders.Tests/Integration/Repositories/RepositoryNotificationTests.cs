using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryNotificationTests : IDisposable
{
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
        using var cmd = new SqliteCommand(
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
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void SaveNotification_ShouldReturnTrue_WhenNotificationIsSaved()
    {
        InsertTestClient(1);
        var notification = CreateTestNotification(1);

        var result = this.repository.SaveNotification(notification);

        result.Should().BeTrue();
    }

    [Fact]
    public void SaveNotification_ShouldInsertNotificationIntoDatabase()
    {
        InsertTestClient(1);
        var notification = CreateTestNotification(1);

        this.repository.SaveNotification(notification);

        var count = GetNotificationCount(1);
        count.Should().Be(1);
    }

    [Fact]
    public void SaveNotification_ShouldSaveAllNotificationProperties()
    {
        InsertTestClient(1);
        var notification = new Notification
        {
            ClientId = 1,
            Title = "Test Title",
            Message = "Test Message",
            Type = NotificationType.Warning,
            RelatedId = 42,
            DateCreated = DateTime.Parse("2024-01-15T10:30:00"),
            IsRead = false
        };

        this.repository.SaveNotification(notification);

        var saved = GetNotificationFromDatabase(1);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Title");
        saved.Message.Should().Be("Test Message");
        saved.Type.Should().Be("Warning");
        saved.RelatedId.Should().Be(42);
        saved.IsRead.Should().BeFalse();
    }

    [Fact]
    public void SaveNotification_ShouldSaveReadNotification_WhenIsReadIsTrue()
    {
        InsertTestClient(1);
        var notification = CreateTestNotification(1);
        notification.IsRead = true;

        this.repository.SaveNotification(notification);

        var saved = GetNotificationFromDatabase(1);
        saved!.IsRead.Should().BeTrue();
    }

    [Fact]
    public void SaveNotification_ShouldSaveAllNotificationTypes()
    {
        InsertTestClient(1);

        var types = new[] { NotificationType.Info, NotificationType.Warning, NotificationType.Plateau, NotificationType.Overload };
        
        foreach (var type in types)
        {
            var notification = CreateTestNotification(1);
            notification.Type = type;
            this.repository.SaveNotification(notification);
        }

        var count = GetNotificationCount(1);
        count.Should().Be(4);
    }

    [Fact]
    public void SaveNotification_ShouldSaveMultipleNotificationsForSameClient()
    {
        InsertTestClient(1);

        var notification1 = CreateTestNotification(1, "First");
        var notification2 = CreateTestNotification(1, "Second");
        var notification3 = CreateTestNotification(1, "Third");

        this.repository.SaveNotification(notification1);
        this.repository.SaveNotification(notification2);
        this.repository.SaveNotification(notification3);

        var count = GetNotificationCount(1);
        count.Should().Be(3);
    }

    [Fact]
    public void SaveNotification_ShouldHandleSpecialCharactersInTitleAndMessage()
    {
        InsertTestClient(1);
        var notification = CreateTestNotification(1);
        notification.Title = "Test's \"Title\" with <special> & chars";
        notification.Message = "Message with 'quotes' and \"double quotes\" & symbols";

        this.repository.SaveNotification(notification);

        var saved = GetNotificationFromDatabase(1);
        saved!.Title.Should().Be("Test's \"Title\" with <special> & chars");
        saved.Message.Should().Be("Message with 'quotes' and \"double quotes\" & symbols");
    }

    [Fact]
    public void SaveNotification_ShouldHandleEmptyTitleAndMessage()
    {
        InsertTestClient(1);
        var notification = CreateTestNotification(1);
        notification.Title = string.Empty;
        notification.Message = string.Empty;

        var result = this.repository.SaveNotification(notification);

        result.Should().BeTrue();
        var saved = GetNotificationFromDatabase(1);
        saved!.Title.Should().BeEmpty();
        saved.Message.Should().BeEmpty();
    }

    [Fact]
    public void SaveNotification_ShouldHandleLongTitleAndMessage()
    {
        InsertTestClient(1);
        var notification = CreateTestNotification(1);
        notification.Title = new string('A', 500);
        notification.Message = new string('B', 1000);

        var result = this.repository.SaveNotification(notification);

        result.Should().BeTrue();
        var saved = GetNotificationFromDatabase(1);
        saved!.Title.Should().HaveLength(500);
        saved.Message.Should().HaveLength(1000);
    }

    [Fact]
    public void GetNotifications_ShouldReturnEmptyList_WhenNoNotificationsExist()
    {
        InsertTestClient(1);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().BeEmpty();
    }

    [Fact]
    public void GetNotifications_ShouldReturnAllNotificationsForClient()
    {
        InsertTestClient(1);
        InsertNotification(1, "Title 1", "Message 1", "Info", 1, DateTime.UtcNow, false);
        InsertNotification(1, "Title 2", "Message 2", "Warning", 2, DateTime.UtcNow, false);
        InsertNotification(1, "Title 3", "Message 3", "Plateau", 3, DateTime.UtcNow, true);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(3);
    }

    [Fact]
    public void GetNotifications_ShouldReturnNotificationsOnlyForSpecificClient()
    {
        InsertTestClient(1);
        InsertTestClient(2);
        InsertNotification(1, "Client 1 Notification", "Message", "Info", 1, DateTime.UtcNow, false);
        InsertNotification(2, "Client 2 Notification", "Message", "Info", 2, DateTime.UtcNow, false);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Be("Client 1 Notification");
        notifications[0].ClientId.Should().Be(1);
    }

    [Fact]
    public void GetNotifications_ShouldOrderByDateDescending()
    {
        InsertTestClient(1);
        var oldDate = DateTime.Parse("2024-01-01T10:00:00");
        var middleDate = DateTime.Parse("2024-01-15T10:00:00");
        var recentDate = DateTime.Parse("2024-01-30T10:00:00");

        InsertNotification(1, "Old", "Message", "Info", 1, oldDate, false);
        InsertNotification(1, "Recent", "Message", "Info", 2, recentDate, false);
        InsertNotification(1, "Middle", "Message", "Info", 3, middleDate, false);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(3);
        notifications[0].Title.Should().Be("Recent");
        notifications[1].Title.Should().Be("Middle");
        notifications[2].Title.Should().Be("Old");
    }

    [Fact]
    public void GetNotifications_ShouldMapAllPropertiesCorrectly()
    {
        InsertTestClient(1);
        var date = DateTime.Parse("2024-01-15T10:30:45");
        InsertNotification(1, "Test Title", "Test Message", "Warning", 42, date, true);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(1);
        var notification = notifications[0];
        notification.Id.Should().BeGreaterThan(0);
        notification.ClientId.Should().Be(1);
        notification.Title.Should().Be("Test Title");
        notification.Message.Should().Be("Test Message");
        notification.Type.Should().Be(NotificationType.Warning);
        notification.RelatedId.Should().Be(42);
        notification.DateCreated.Should().BeCloseTo(date, TimeSpan.FromSeconds(1));
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void GetNotifications_ShouldParseAllNotificationTypes()
    {
        InsertTestClient(1);
        var date = DateTime.UtcNow;
        InsertNotification(1, "Info", "Message", "Info", 1, date, false);
        InsertNotification(1, "Warning", "Message", "Warning", 2, date, false);
        InsertNotification(1, "Plateau", "Message", "Plateau", 3, date, false);
        InsertNotification(1, "Overload", "Message", "Overload", 4, date, false);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(4);
        notifications.Should().Contain(n => n.Type == NotificationType.Info);
        notifications.Should().Contain(n => n.Type == NotificationType.Warning);
        notifications.Should().Contain(n => n.Type == NotificationType.Plateau);
        notifications.Should().Contain(n => n.Type == NotificationType.Overload);
    }

    [Fact]
    public void GetNotifications_ShouldHandleBothReadAndUnreadNotifications()
    {
        InsertTestClient(1);
        var date = DateTime.UtcNow;
        InsertNotification(1, "Unread", "Message", "Info", 1, date, false);
        InsertNotification(1, "Read", "Message", "Info", 2, date, true);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(2);
        notifications.Should().Contain(n => n.IsRead == false && n.Title == "Unread");
        notifications.Should().Contain(n => n.IsRead == true && n.Title == "Read");
    }

    [Fact]
    public void GetNotifications_ShouldHandleNotificationsWithDifferentRelatedIds()
    {
        InsertTestClient(1);
        var date = DateTime.UtcNow;
        InsertNotification(1, "Related 1", "Message", "Info", 100, date, false);
        InsertNotification(1, "Related 2", "Message", "Info", 200, date, false);
        InsertNotification(1, "Related 3", "Message", "Info", 0, date, false);

        var notifications = this.repository.GetNotifications(1);

        notifications.Should().HaveCount(3);
        notifications.Should().Contain(n => n.RelatedId == 100);
        notifications.Should().Contain(n => n.RelatedId == 200);
        notifications.Should().Contain(n => n.RelatedId == 0);
    }

    [Fact]
    public void IntegrationTest_SaveAndRetrieveMultipleNotifications()
    {
        InsertTestClient(1);
        InsertTestClient(2);

        var notification1 = new Notification("Title 1", "Message 1", NotificationType.Info, 1) { ClientId = 1 };
        var notification2 = new Notification("Title 2", "Message 2", NotificationType.Warning, 2) { ClientId = 1, IsRead = true };
        var notification3 = new Notification("Title 3", "Message 3", NotificationType.Plateau, 3) { ClientId = 2 };

        this.repository.SaveNotification(notification1);
        this.repository.SaveNotification(notification2);
        this.repository.SaveNotification(notification3);

        var client1Notifications = this.repository.GetNotifications(1);
        var client2Notifications = this.repository.GetNotifications(2);

        client1Notifications.Should().HaveCount(2);
        client2Notifications.Should().HaveCount(1);

        client1Notifications[0].IsRead.Should().BeTrue();
        client1Notifications[1].IsRead.Should().BeFalse();
    }

    [Fact]
    public void IntegrationTest_SaveNotificationAndVerifyAllFields()
    {
        InsertTestClient(1);
        var originalDate = DateTime.Parse("2024-01-15T14:30:00");
        var notification = new Notification
        {
            ClientId = 1,
            Title = "Achievement Unlocked",
            Message = "You completed your first workout!",
            Type = NotificationType.Info,
            RelatedId = 5,
            DateCreated = originalDate,
            IsRead = false
        };

        this.repository.SaveNotification(notification);
        var retrieved = this.repository.GetNotifications(1);

        retrieved.Should().HaveCount(1);
        var saved = retrieved[0];
        saved.ClientId.Should().Be(1);
        saved.Title.Should().Be("Achievement Unlocked");
        saved.Message.Should().Be("You completed your first workout!");
        saved.Type.Should().Be(NotificationType.Info);
        saved.RelatedId.Should().Be(5);
        saved.DateCreated.Should().BeCloseTo(originalDate, TimeSpan.FromSeconds(1));
        saved.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainDataIntegrity()
    {
        InsertTestClient(1);
        InsertTestClient(2);

        for (int i = 0; i < 5; i++)
        {
            var notification = CreateTestNotification(1, $"Notification {i}");
            this.repository.SaveNotification(notification);
        }

        for (int i = 0; i < 3; i++)
        {
            var notification = CreateTestNotification(2, $"Notification {i}");
            this.repository.SaveNotification(notification);
        }

        var client1Count = this.repository.GetNotifications(1).Count;
        var client2Count = this.repository.GetNotifications(2).Count;
        var totalCount = GetTotalNotificationCount();

        client1Count.Should().Be(5);
        client2Count.Should().Be(3);
        totalCount.Should().Be(8);
    }

    private void InsertTestClient(int clientId)
    {
        using var cmd = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@id, @uid, 1, 75.0, 180.0)",
            this.connection);
        cmd.Parameters.AddWithValue("@id", clientId);
        cmd.Parameters.AddWithValue("@uid", clientId + 1000);
        cmd.ExecuteNonQuery();
    }

    private void InsertNotification(int clientId, string title, string message, string type, int relatedId, DateTime dateCreated, bool isRead)
    {
        using var cmd = new SqliteCommand(
            @"INSERT INTO NOTIFICATION (client_id, title, message, type, related_id, date_created, is_read)
              VALUES (@clientId, @title, @message, @type, @relatedId, @dateCreated, @isRead)",
            this.connection);
        cmd.Parameters.AddWithValue("@clientId", clientId);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@message", message);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.Parameters.AddWithValue("@relatedId", relatedId);
        cmd.Parameters.AddWithValue("@dateCreated", dateCreated.ToString("o"));
        cmd.Parameters.AddWithValue("@isRead", isRead ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    private Notification CreateTestNotification(int clientId, string titleSuffix = "")
    {
        return new Notification
        {
            ClientId = clientId,
            Title = string.IsNullOrEmpty(titleSuffix) ? "Test Notification" : $"Test Notification {titleSuffix}",
            Message = "Test message content",
            Type = NotificationType.Info,
            RelatedId = 1,
            DateCreated = DateTime.UtcNow,
            IsRead = false
        };
    }

    private int GetNotificationCount(int clientId)
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM NOTIFICATION WHERE client_id = @clientId",
            this.connection);
        cmd.Parameters.AddWithValue("@clientId", clientId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int GetTotalNotificationCount()
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM NOTIFICATION",
            this.connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private NotificationData? GetNotificationFromDatabase(int clientId)
    {
        using var cmd = new SqliteCommand(
            @"SELECT title, message, type, related_id, is_read
              FROM NOTIFICATION 
              WHERE client_id = @clientId 
              ORDER BY id DESC 
              LIMIT 1",
            this.connection);
        cmd.Parameters.AddWithValue("@clientId", clientId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new NotificationData
        {
            Title = reader.GetString(0),
            Message = reader.GetString(1),
            Type = reader.GetString(2),
            RelatedId = reader.GetInt32(3),
            IsRead = reader.GetInt32(4) != 0
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

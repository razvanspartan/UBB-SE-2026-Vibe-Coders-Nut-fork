using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryNotificationTests : IDisposable
{
    private const int FirstTestClientId = 1;
    private const int SecondTestClientId = 2;
    private const int ThirdTestClientId = 3;
    private const int SqliteFalse = 0;
    private const int FirstNotificationCount = 5;
    private const int SecondNotificationCount = 3;
    private const int TotalNotificationCount = 8;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryNotification repository;
    private readonly TestDataHelper testDataHelper;

    public RepositoryNotificationTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);

        this.repository = new RepositoryNotification(this.connectionString);
        this.testDataHelper = new TestDataHelper(this.connection);

        this.testDataHelper.SetupTrainer();
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    [Fact]
    public void GetNotifications_ShouldReturnNotificationsOnlyForSpecificClient()
    {
        this.testDataHelper.InsertClient(FirstTestClientId);
        this.testDataHelper.InsertClient(SecondTestClientId);
        this.testDataHelper.InsertNotification(FirstTestClientId, "Client 1 Notification", "Message", "Info", FirstTestClientId, DateTime.UtcNow, false);
        this.testDataHelper.InsertNotification(SecondTestClientId, "Client 2 Notification", "Message", "Info", SecondTestClientId, DateTime.UtcNow, false);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().HaveCount(FirstTestClientId);
        notifications[SqliteFalse].Title.Should().Be("Client 1 Notification");
        notifications[SqliteFalse].ClientId.Should().Be(FirstTestClientId);
    }

    [Fact]
    public void GetNotifications_ShouldOrderByDateDescending()
    {
        this.testDataHelper.InsertClient(FirstTestClientId);
        var oldDate = DateTime.Parse("2024-01-01T10:00:00");
        var middleDate = DateTime.Parse("2024-01-15T10:00:00");
        var recentDate = DateTime.Parse("2024-01-30T10:00:00");

        this.testDataHelper.InsertNotification(FirstTestClientId, "Old", "Message", "Info", FirstTestClientId, oldDate, false);
        this.testDataHelper.InsertNotification(FirstTestClientId, "Recent", "Message", "Info", SecondTestClientId, recentDate, false);
        this.testDataHelper.InsertNotification(FirstTestClientId, "Middle", "Message", "Info", ThirdTestClientId, middleDate, false);

        var notifications = this.repository.GetNotifications(FirstTestClientId);

        notifications.Should().HaveCount(ThirdTestClientId);
        notifications[SqliteFalse].Title.Should().Be("Recent");
        notifications[FirstTestClientId].Title.Should().Be("Middle");
        notifications[SecondTestClientId].Title.Should().Be("Old");
    }

    [Fact]
    public void IntegrationTest_SaveAndRetrieveMultipleNotifications()
    {
        this.testDataHelper.InsertClient(FirstTestClientId);
        this.testDataHelper.InsertClient(SecondTestClientId);

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
        this.testDataHelper.InsertClient(FirstTestClientId);
        this.testDataHelper.InsertClient(SecondTestClientId);

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
        var totalCount = this.testDataHelper.GetTotalNotificationCount();

        client1Count.Should().Be(FirstNotificationCount);
        client2Count.Should().Be(SecondNotificationCount);
        totalCount.Should().Be(TotalNotificationCount);
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
}

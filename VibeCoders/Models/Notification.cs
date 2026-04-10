namespace VibeCoders.Models;

/// <summary>
/// Defines the categories for system notifications.
/// </summary>
public enum NotificationType
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Warning message.</summary>
    Warning,

    /// <summary>Plateau alert.</summary>
    Plateau,

    /// <summary>Overload alert.</summary>
    Overload
}

public class Notification
{
    public Notification()
    {
        this.DateCreated = DateTime.Now;
    }

    public Notification(string title, string message, NotificationType type, int relatedId)
    {
        this.Title = title;
        this.Message = message;
        this.Type = type;
        this.RelatedId = relatedId;
        this.DateCreated = DateTime.Now;
        this.IsRead = false;
    }

    public int Id { get; set; }

    public int ClientId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public int RelatedId { get; set; }

    public DateTime DateCreated { get; set; }

    public bool IsRead { get; set; }
}
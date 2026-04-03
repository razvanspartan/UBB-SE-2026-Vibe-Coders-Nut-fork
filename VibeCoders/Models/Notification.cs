namespace VibeCoders.Models;

public enum NotificationType
{
    Info,
    Warning,
    Plateau,
    Overload
}

public class Notification
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int RelatedId { get; set; }
    public DateTime DateCreated { get; set; }
    public bool IsRead { get; set; }

    public Notification()
    {
        DateCreated = DateTime.Now;
    }

    public Notification(string title, string message, NotificationType type, int relatedId)
    {
        Title = title;
        Message = message;
        Type = type;
        RelatedId = relatedId;
        DateCreated = DateTime.Now;
        IsRead = false;
    }
}
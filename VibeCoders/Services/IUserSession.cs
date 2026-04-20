namespace VibeCoders.Services;

public interface IUserSession
{
    long CurrentUserId { get; set; }
    long CurrentClientId { get; set; }
}
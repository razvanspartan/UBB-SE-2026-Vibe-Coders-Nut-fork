namespace VibeCoders.Services;

public sealed class UserSession : IUserSession
{
    public long CurrentUserId { get; set; } = 1;

    public long CurrentClientId { get; set; } = 1;
}

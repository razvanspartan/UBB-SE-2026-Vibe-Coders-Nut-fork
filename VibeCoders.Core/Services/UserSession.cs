namespace VibeCoders.Services;

/// <summary>
/// Default session stub. All analytics queries scope to this user id until
/// real authentication is wired in.
/// </summary>
public sealed class UserSession : IUserSession
{
    public long CurrentUserId { get; set; } = 1;
}

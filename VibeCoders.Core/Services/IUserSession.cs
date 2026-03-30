namespace VibeCoders.Services;

/// <summary>
/// Provides the identity of the currently signed-in client.
/// Replace with a real auth provider when login is integrated.
/// </summary>
public interface IUserSession
{
    /// <summary>Stable user key used to scope analytics queries.</summary>
    long CurrentUserId { get; }
}

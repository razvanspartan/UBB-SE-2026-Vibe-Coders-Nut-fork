namespace VibeCoders.Services;

/// <summary>
/// Provides the identity of the currently signed-in client.
/// Replace with a real auth provider when login is integrated.
/// </summary>
public interface IUserSession
{
    /// <summary>Scopes analytics rows (<c>analytics_workout_log.user_id</c>); same numeric scope as <see cref="CurrentClientId"/> in demo mode.</summary>
    long CurrentUserId { get; }

    /// <summary><c>CLIENT.client_id</c> for the active client (workouts, logs, achievements, calendar).</summary>
    long CurrentClientId { get; }
}

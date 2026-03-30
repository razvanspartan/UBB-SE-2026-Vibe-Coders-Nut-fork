using VibeCoders.Models;

namespace VibeCoders.Services;

/// <summary>
/// Accepts a completed workout session, persists it to the analytics store,
/// and signals the dashboard to refresh. Intended to be called from whatever
/// module owns the "finish workout" flow.
/// </summary>
public interface IWorkoutDataForwarder
{
    /// <summary>
    /// Saves the completed workout and fires a dashboard refresh event.
    /// Returns the generated workout_log id.
    /// </summary>
    Task<int> ForwardCompletedWorkoutAsync(long userId, WorkoutLog log, CancellationToken cancellationToken = default);
}

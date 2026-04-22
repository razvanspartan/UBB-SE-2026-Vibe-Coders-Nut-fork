using VibeCoders.Models;

namespace VibeCoders.Services;

public interface IWorkoutDataForwarder
{
    Task<int> ForwardCompletedWorkoutAsync(long userId, WorkoutLog log, CancellationToken cancellationToken = default);
}
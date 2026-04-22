using VibeCoders.Models;

namespace VibeCoders.Services;

public interface ICalendarWorkoutCatalogService
{
    Task<IReadOnlyList<WorkoutTemplate>> GetAvailableWorkoutsAsync(int clientId, TimeSpan timeout);
    IReadOnlyList<WorkoutTemplate> GetFallbackWorkouts(int clientId);
}

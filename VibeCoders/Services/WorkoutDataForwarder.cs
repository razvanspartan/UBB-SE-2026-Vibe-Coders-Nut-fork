using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;

namespace VibeCoders.Services;

public sealed class WorkoutDataForwarder : IWorkoutDataForwarder
{
    private readonly IWorkoutAnalyticsStore store;
    private readonly IAnalyticsDashboardRefreshBus refreshBus;

    private const float LightThreshold = 3.0f;
    private const float ModerateThreshold = 6.0f;

    private const string LightIntensity = "light";
    private const string ModerateIntensity = "moderate";
    private const string IntenseIntensity = "intense";

    public WorkoutDataForwarder(
        IWorkoutAnalyticsStore store,
        IAnalyticsDashboardRefreshBus refreshBus)
    {
        this.store = store;
        this.refreshBus = refreshBus;
    }

    public async Task<int> ForwardCompletedWorkoutAsync(
        long userId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        log.TotalCaloriesBurned = log.Exercises.Sum(e => e.ExerciseCaloriesBurned);

        if (log.Exercises.Count > 0)
        {
            log.AverageMetabolicEquivalent = log.Exercises.Average(e => e.MetabolicEquivalent);
            log.IntensityTag = CalculateIntensityTag(log.AverageMetabolicEquivalent);
        }

        var logId = await store.SaveWorkoutAsync(log.ClientId, log, cancellationToken);

        refreshBus.RequestRefresh();

        return logId;
    }

    private static string CalculateIntensityTag(float averageMetabolicEquivalent)
    {
        if (averageMetabolicEquivalent < LightThreshold)
        {
            return LightIntensity;
        }

        if (averageMetabolicEquivalent < ModerateThreshold)
        {
            return ModerateIntensity;
        }

        return IntenseIntensity;
    }
}

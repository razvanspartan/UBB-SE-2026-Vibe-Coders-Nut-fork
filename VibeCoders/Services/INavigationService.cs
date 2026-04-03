namespace VibeCoders.Services;

public interface INavigationService
{
    void NavigateToClientDashboard(bool requestRefresh);

    void NavigateToCalendarIntegration();

    void NavigateToRankShowcase();

    void NavigateToActiveWorkout();

    void NavigateToWorkoutLogs();
    void GoBack();
}
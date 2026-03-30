namespace VibeCoders.Services;

/// <summary>
/// Application-level navigation abstraction.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the client analytics dashboard.
    /// When <paramref name="requestRefresh"/> is true, the dashboard reloads
    /// its data after navigation completes.
    /// </summary>
    void NavigateToClientDashboard(bool requestRefresh);

    /// <summary>
    /// Navigates to the calendar integration page.
    /// </summary>
    void NavigateToCalendarIntegration();

    /// <summary>
    /// Navigates to the rank showcase (level, rank title, full achievement list including locked).
    /// </summary>
    void NavigateToRankShowcase();

    /// <summary>
    /// Navigates to the active workout session page.
    /// </summary>
    void NavigateToActiveWorkout();

    /// <summary>
    /// Navigates back to the previous page when the frame history allows it.
    /// </summary>
    void GoBack();
}

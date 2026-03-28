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
}

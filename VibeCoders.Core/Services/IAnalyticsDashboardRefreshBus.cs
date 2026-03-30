namespace VibeCoders.Services;

/// <summary>
/// Cross-feature event bus used to signal the analytics dashboard to reload.
/// The workout completion flow publishes; the dashboard ViewModel subscribes.
/// </summary>
public interface IAnalyticsDashboardRefreshBus
{
    event EventHandler? RefreshRequested;

    void RequestRefresh();
}

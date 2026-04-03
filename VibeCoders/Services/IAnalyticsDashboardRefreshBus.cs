namespace VibeCoders.Services;

public interface IAnalyticsDashboardRefreshBus
{
    event EventHandler? RefreshRequested;

    void RequestRefresh();
}

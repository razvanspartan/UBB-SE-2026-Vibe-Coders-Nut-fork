namespace VibeCoders.Services;

public sealed class AnalyticsDashboardRefreshBus : IAnalyticsDashboardRefreshBus
{
    public event EventHandler? RefreshRequested;

    public void RequestRefresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}

using Microsoft.UI.Xaml.Controls;

namespace VibeCoders.Services;

/// <inheritdoc />
public sealed class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly IAnalyticsDashboardRefreshBus _refreshBus;

    public NavigationService(IAnalyticsDashboardRefreshBus refreshBus)
    {
        _refreshBus = refreshBus;
    }

    /// <summary>
    /// Binds the root navigation frame. Must be called once from MainWindow
    /// after it initialises its content.
    /// </summary>
    public void AttachFrame(Frame frame)
    {
        _frame = frame;
    }

    /// <inheritdoc />
    public void NavigateToClientDashboard(bool requestRefresh)
    {
        if (_frame is null)
        {
            return;
        }

        _frame.Navigate(typeof(Views.ClientDashboardPage));

        if (requestRefresh)
        {
            _refreshBus.RequestRefresh();
        }
    }
}

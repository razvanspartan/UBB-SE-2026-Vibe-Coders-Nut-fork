using Microsoft.UI.Xaml.Controls;
using VibeCoders.Views;

namespace VibeCoders.Services;

/// <inheritdoc cref="INavigationService" />
/// <remarks>Resolves WinUI <see cref="Microsoft.UI.Xaml.Controls.Page"/> types for the shell <see cref="Microsoft.UI.Xaml.Controls.Frame"/>.</remarks>
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
        if (_frame is null) return;

        _frame.Navigate(typeof(ClientDashboardPage));

        if (requestRefresh)
        {
            _refreshBus.RequestRefresh();
        }
    }

    /// <inheritdoc />
    public void NavigateToCalendarIntegration()
    {
        if (_frame is null) return;

        _frame.Navigate(typeof(CalendarIntegrationPage));
    }

    /// <inheritdoc />
    public void NavigateToRankShowcase()
    {
        if (_frame is null)
        {
            return;
        }

        _frame.Navigate(typeof(RankShowcasePage));
    }

    /// <inheritdoc />
    public void NavigateToActiveWorkout()
    {
        if (_frame is null) return;

        _frame.Navigate(typeof(Views.ActiveWorkoutPage));
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (_frame is null) return;

        if (_frame.CanGoBack)
        {
            _frame.GoBack();
        }
    }
}
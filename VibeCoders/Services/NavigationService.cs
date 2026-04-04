using Microsoft.UI.Xaml.Controls;
using VibeCoders.Views;

namespace VibeCoders.Services;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly IAnalyticsDashboardRefreshBus _refreshBus;

    public NavigationService(IAnalyticsDashboardRefreshBus refreshBus)
    {
        _refreshBus = refreshBus;
    }

    public void AttachFrame(Frame frame)
    {
        _frame = frame;
    }

    public void NavigateToClientDashboard(bool requestRefresh)
    {
        if (_frame is null) return;
        _frame.Navigate(typeof(ClientDashboardPage));
        if (requestRefresh) _refreshBus.RequestRefresh();
    }

    public void NavigateToCalendarIntegration()
    {
        if (_frame is null) return;
        _frame.Navigate(typeof(CalendarIntegrationPage));
    }

    public void NavigateToRankShowcase()
    {
        if (_frame is null) return;
        _frame.Navigate(typeof(RankShowcasePage));
    }

    public void NavigateToActiveWorkout()
    {
        if (_frame is null) return;
        _frame.Navigate(typeof(ActiveWorkoutPage));
    }

    public void NavigateToWorkoutLogs()
    {
        if (_frame is null) return;
        _frame.Navigate(typeof(WorkoutLogsPage));
    }

    public void GoBack()
    {
        if (_frame is null) return;
        if (_frame.CanGoBack) _frame.GoBack();
    }

    public void NavigateToTrainerDashboard()
    {
        if (_frame is null)
        {
            return;
        }

        _frame?.Navigate(typeof(Views.TrainerDashboardView));

    }

    public void NavigateToClientProfile(int clientId)
    {
        if (_frame == null)
        {
            return;
        }
        _frame.Navigate(typeof(ClientProfileView), clientId);
    }

}

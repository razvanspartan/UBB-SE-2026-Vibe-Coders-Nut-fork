using Microsoft.UI.Xaml.Controls;
using VibeCoders.Views;

namespace VibeCoders.Services;

public sealed class NavigationService : INavigationService
{
    private Frame? frame;
    private readonly IAnalyticsDashboardRefreshBus refreshBus;

    public NavigationService(IAnalyticsDashboardRefreshBus refreshBus)
    {
        this.refreshBus = refreshBus;
    }

    public void AttachFrame(Frame frame)
    {
        this.frame = frame;
    }

    public void NavigateToClientDashboard(bool requestRefresh)
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(ClientDashboardPage));

        if (requestRefresh)
        {
            refreshBus.RequestRefresh();
        }
    }

    public void NavigateToCalendarIntegration()
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(CalendarIntegrationPage));
    }

    public void NavigateToRankShowcase()
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(RankShowcasePage));
    }

    public void NavigateToActiveWorkout(int clientId = 0)
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(ActiveWorkoutPage), clientId);
    }

    public void NavigateToWorkoutLogs()
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(WorkoutLogsPage));
    }

    public void GoBack()
    {
        if (frame is null)
        {
            return;
        }

        if (frame.CanGoBack)
        {
            frame.GoBack();
        }
    }

    public void NavigateToTrainerDashboard()
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(TrainerDashboardView));
    }

    public void NavigateToClientProfile(int clientId)
    {
        if (frame is null)
        {
            return;
        }

        frame.Navigate(typeof(ClientProfileView), clientId);
    }
}

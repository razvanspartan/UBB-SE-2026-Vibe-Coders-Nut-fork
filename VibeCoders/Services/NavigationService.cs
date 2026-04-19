namespace VibeCoders.Services
{
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.Views;

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
            frame?.Navigate(typeof(ClientDashboardPage));

            if (requestRefresh)
            {
                refreshBus.RequestRefresh();
            }
        }

        public void NavigateToCalendarIntegration()
        {
            frame?.Navigate(typeof(CalendarIntegrationPage));
        }

        public void NavigateToRankShowcase()
        {
            frame?.Navigate(typeof(RankShowcasePage));
        }

        public void NavigateToActiveWorkout(int clientId = 0)
        {
            frame?.Navigate(typeof(ActiveWorkoutPage), clientId);
        }

        public void NavigateToWorkoutLogs()
        {
            frame?.Navigate(typeof(WorkoutLogsPage));
        }

        public void GoBack()
        {
            if (frame?.CanGoBack == true)
            {
                frame.GoBack();
            }
        }

        public void NavigateToTrainerDashboard()
        {
            frame?.Navigate(typeof(TrainerDashboardView));
        }

        public void NavigateToClientProfile(int clientId)
        {
            frame?.Navigate(typeof(ClientProfileView), clientId);
        }
    }
}


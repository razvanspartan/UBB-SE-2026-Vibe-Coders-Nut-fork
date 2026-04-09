namespace VibeCoders.Services
{
    using System;
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.Views;

    public sealed class NavigationService : INavigationService
    {
        private const int DefaultClientId = 0;

        private readonly IAnalyticsDashboardRefreshBus refreshBus;
        private Frame? frame;

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
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(ClientDashboardPage));

            if (requestRefresh)
            {
                this.refreshBus.RequestRefresh();
            }
        }

        public void NavigateToCalendarIntegration()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(CalendarIntegrationPage));
        }

        public void NavigateToRankShowcase()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(RankShowcasePage));
        }

        public void NavigateToActiveWorkout(int clientId = NavigationService.DefaultClientId)
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(ActiveWorkoutPage), clientId);
        }

        public void NavigateToWorkoutLogs()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(WorkoutLogsPage));
        }

        public void GoBack()
        {
            if (this.frame is null)
            {
                return;
            }

            if (this.frame.CanGoBack)
            {
                this.frame.GoBack();
            }
        }

        public void NavigateToTrainerDashboard()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(TrainerDashboardView));
        }

        public void NavigateToClientProfile(int clientId)
        {
            if (this.frame == null)
            {
                return;
            }

            this.frame.Navigate(typeof(ClientProfileView), clientId);
        }
    }
}
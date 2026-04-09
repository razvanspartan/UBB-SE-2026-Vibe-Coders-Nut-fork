namespace VibeCoders.Services
{
    using System;
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.Views;

    /// <summary>
    /// Service responsible for handling navigation within the application.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private const int DefaultClientId = 0;

        private readonly IAnalyticsDashboardRefreshBus refreshBus;
        private Frame? frame;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        /// <param name="refreshBus">The analytics dashboard refresh bus.</param>
        public NavigationService(IAnalyticsDashboardRefreshBus refreshBus)
        {
            this.refreshBus = refreshBus;
        }

        /// <summary>
        /// Attaches a Frame to the navigation service for use in navigation.
        /// </summary>
        /// <param name="frame">The frame to attach.</param>
        public void AttachFrame(Frame frame)
        {
            this.frame = frame;
        }

        /// <summary>
        /// Navigates to the client dashboard page.
        /// </summary>
        /// <param name="requestRefresh">True if a refresh should be requested upon navigation; otherwise, false.</param>
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

        /// <summary>
        /// Navigates to the calendar integration page.
        /// </summary>
        public void NavigateToCalendarIntegration()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(CalendarIntegrationPage));
        }

        /// <summary>
        /// Navigates to the rank showcase page.
        /// </summary>
        public void NavigateToRankShowcase()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(RankShowcasePage));
        }

        /// <summary>
        /// Navigates to the active workout page.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
        public void NavigateToActiveWorkout(int clientId = NavigationService.DefaultClientId)
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(ActiveWorkoutPage), clientId);
        }

        /// <summary>
        /// Navigates to the workout logs page.
        /// </summary>
        public void NavigateToWorkoutLogs()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(WorkoutLogsPage));
        }

        /// <summary>
        /// Navigates back to the previous page in the navigation stack.
        /// </summary>
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

        /// <summary>
        /// Navigates to the trainer dashboard view.
        /// </summary>
        public void NavigateToTrainerDashboard()
        {
            if (this.frame is null)
            {
                return;
            }

            this.frame.Navigate(typeof(TrainerDashboardView));
        }

        /// <summary>
        /// Navigates to the client profile view.
        /// </summary>
        /// <param name="clientId">The client's ID.</param>
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
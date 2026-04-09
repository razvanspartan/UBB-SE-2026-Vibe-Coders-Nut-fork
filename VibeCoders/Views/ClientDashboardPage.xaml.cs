#pragma warning disable SA1600
#pragma warning disable SA1601

namespace VibeCoders.Views
{
    using System.ComponentModel;
    using LiveChartsCore.SkiaSharpView.WinUI;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.Services;
    using VibeCoders.ViewModels;

    public sealed partial class ClientDashboardPage : Page
    {
        private readonly CartesianChart chart;
        private readonly IAchievementUnlockedBus achievementBus;

        public ClientDashboardPage()
        {
            this.ViewModel = App.GetService<ClientDashboardViewModel>();
            this.DataContext = this.ViewModel;
            this.InitializeComponent();

            this.achievementBus = App.GetService<IAchievementUnlockedBus>();
            this.achievementBus.AchievementUnlocked += this.OnAchievementUnlocked;

            this.chart = new CartesianChart
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            this.SyncChartToViewModel();
            this.ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
            this.ChartContainer.Children.Add(this.chart);

            this.Unloaded += this.Page_Unloaded;
        }

        public ClientDashboardViewModel ViewModel { get; }

        private void SeeAllAchievements_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            int clientId = (int)App.GetService<IUserSession>().CurrentClientId;
            this.Frame.Navigate(typeof(AchievementsPage), clientId);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            await this.ViewModel.LoadInitialAsync();

            WorkoutUiState workoutUiState = App.GetService<WorkoutUiState>();
            string? progressionHeadsUpNote = workoutUiState.ProgressionHeadsUp;
            if (!string.IsNullOrWhiteSpace(progressionHeadsUpNote))
            {
                this.ProgressionInfoBar.Message = progressionHeadsUpNote;
                this.ProgressionInfoBar.IsOpen = true;
                workoutUiState.ProgressionHeadsUp = null;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.achievementBus.AchievementUnlocked -= this.OnAchievementUnlocked;
            this.ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
            this.ChartContainer.Children.Remove(this.chart);
        }

        private void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs achievementUnlockedEventArgs)
        {
            this.ViewModel.ReloadAchievementsPreview();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName is nameof(ClientDashboardViewModel.ChartSeries) or nameof(ClientDashboardViewModel.ChartXAxes))
            {
                this.SyncChartToViewModel();
            }
        }

        private void SyncChartToViewModel()
        {
            this.chart.Series = this.ViewModel.ChartSeries;
            this.chart.XAxes = this.ViewModel.ChartXAxes;
        }
    }
}
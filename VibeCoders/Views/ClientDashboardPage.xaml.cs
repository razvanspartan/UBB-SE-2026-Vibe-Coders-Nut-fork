namespace VibeCoders.Views;

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

    public ClientDashboardViewModel ViewModel { get; }

    public ClientDashboardPage()
    {
        ViewModel = App.GetService<ClientDashboardViewModel>();
        DataContext = ViewModel;
        InitializeComponent();

        achievementBus = App.GetService<IAchievementUnlockedBus>();
        achievementBus.AchievementUnlocked += OnAchievementUnlocked;

        chart = new CartesianChart
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };

        SyncChartToViewModel();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ChartContainer.Children.Add(chart);

        Unloaded += Page_Unloaded;
    }

    private void SeeAllAchievements_Click(object sender, RoutedEventArgs e)
    {
        var clientId = (int)App.GetService<IUserSession>().CurrentClientId;
        Frame.Navigate(typeof(AchievementsPage), clientId);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadInitialAsync();

        var workoutState = App.GetService<WorkoutUiState>();
        var note = workoutState.ProgressionHeadsUp;
        if (!string.IsNullOrWhiteSpace(note))
        {
            ProgressionInfoBar.Message = note;
            ProgressionInfoBar.IsOpen = true;
            workoutState.ProgressionHeadsUp = null;
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        achievementBus.AchievementUnlocked -= OnAchievementUnlocked;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ChartContainer.Children.Remove(chart);
    }

    private void OnAchievementUnlocked(object? sender, AchievementUnlockedEventArgs e)
    {
        ViewModel.ReloadAchievementsPreview();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.ChartSeries) or nameof(ViewModel.ChartXAxes))
        {
            SyncChartToViewModel();
        }
    }

    private void SyncChartToViewModel()
    {
        chart.Series = ViewModel.ChartSeries;
        chart.XAxes = ViewModel.ChartXAxes;
    }
}

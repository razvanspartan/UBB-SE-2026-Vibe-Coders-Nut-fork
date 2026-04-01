using System.ComponentModel;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class ClientDashboardPage : Page
{
    private readonly CartesianChart _chart;

    public ClientDashboardViewModel ViewModel { get; }

    public ClientDashboardPage()
    {
        ViewModel = App.GetService<ClientDashboardViewModel>();
        DataContext = ViewModel;
        InitializeComponent();

        _chart = new CartesianChart
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };

        SyncChartToViewModel();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ChartContainer.Children.Add(_chart);

        Unloaded += Page_Unloaded;
    }

    private void SeeAllAchievements_Click(object sender, RoutedEventArgs e)
    {
        var clientId = (int)App.GetService<IUserSession>().CurrentUserId;
        Frame.Navigate(typeof(AchievementsPage), clientId);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadInitialAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ChartContainer.Children.Remove(_chart);
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
        _chart.Series = ViewModel.ChartSeries;
        _chart.XAxes = ViewModel.ChartXAxes;
    }
}

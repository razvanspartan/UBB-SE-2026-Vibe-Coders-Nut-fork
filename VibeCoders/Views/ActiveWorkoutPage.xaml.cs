using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.ViewModels;
using System;

namespace VibeCoders.Views;

public sealed partial class ActiveWorkoutPage : Page
{
    public ActiveWorkoutViewModel ViewModel { get; }
    public int ClientId { get; private set; }

    private DispatcherTimer _timer;
    private TimeSpan _elapsed = TimeSpan.Zero;

    public ActiveWorkoutPage()
    {
        ViewModel = App.GetService<ActiveWorkoutViewModel>();
        DataContext = ViewModel;
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) =>
        {
            _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
            WorkoutTimerDisplay.Text = _elapsed.ToString(@"hh\:mm\:ss");
        };
        _timer.Start();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int clientId)
            ClientId = clientId;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _timer.Stop();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadAvailableWorkoutsCommand.Execute(ClientId);
        ViewModel.LoadNotificationsCommand.Execute(ClientId);
    }
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.Models;
using VibeCoders.ViewModels;

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
            WorkoutTimerDisplay.Text = _elapsed.ToString(@"mm\:ss");
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
        ViewModel.LoadNotificationsCommand.Execute(ClientId);
    }

    /// <summary>
    /// Closes the flyout and applies the selected target goals. (#74)
    /// </summary>
    private void ApplyGoalsButton_Click(object sender, RoutedEventArgs e)
    {
        TargetGoalsButton.Flyout.Hide();
        ViewModel.ApplyTargetGoalsCommand.Execute(ClientId);
    }

    /// <summary>
    /// Handles Confirm Deload button click from inside DataTemplate.
    /// Tag="{x:Bind}" passes the Notification as the button's Tag.
    /// </summary>
    private void ConfirmDeloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Notification notification)
        {
            ViewModel.ConfirmDeloadCommand.Execute(notification);
        }
    }
}
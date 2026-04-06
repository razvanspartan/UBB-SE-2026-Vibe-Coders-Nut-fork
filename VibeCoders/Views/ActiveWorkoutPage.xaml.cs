using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class ActiveWorkoutPage : Page
{
    public ActiveWorkoutViewModel ViewModel { get; }
    public int ClientId { get; private set; }

    public ActiveWorkoutPage()
    {
        ViewModel = App.GetService<ActiveWorkoutViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int clientId && clientId != 0)
            ClientId = clientId;
        else
            ClientId = (int)App.GetService<IUserSession>().CurrentClientId;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadNotificationsCommand.Execute(ClientId);
    }

<<<<<<< HEAD
=======
    private async void OpenFocusMode_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsWorkoutStarted) return;

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            FullSizeDesired = true,
            IsPrimaryButtonEnabled = false,
            IsSecondaryButtonEnabled = false
        };

        var focusPage = new FocusModeView(ViewModel, ClientId, dialog);
        dialog.Content = focusPage;
        await dialog.ShowAsync();
    }

>>>>>>> origin/main
    private void ApplyGoalsButton_Click(object sender, RoutedEventArgs e)
    {
        TargetGoalsButton.Flyout.Hide();
        ViewModel.ApplyTargetGoalsCommand.Execute(ClientId);
    }

    private void ConfirmDeloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Notification notification)
            ViewModel.ConfirmDeloadCommand.Execute(notification);
    }

    private void SaveSetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ActiveSetViewModel setVm)
            ViewModel.SaveSetCommand.Execute(setVm);
    }
}

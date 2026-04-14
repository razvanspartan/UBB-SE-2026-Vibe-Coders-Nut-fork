using System.ComponentModel;
using System.Threading.Tasks;
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
    private bool isFocusDialogOpen;

    public ActiveWorkoutPage()
    {
        ViewModel = App.GetService<ActiveWorkoutViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int clientId && clientId != 0)
        {
            ClientId = clientId;
        }
        else
        {
            ClientId = (int)App.GetService<IUserSession>().CurrentClientId;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadNotificationsCommand.Execute(ClientId);
        ViewModel.LoadCustomWorkouts(ClientId);
    }

    private async void OpenFocusMode_Click(object sender, RoutedEventArgs e)
    {
        await OpenFocusModeAsync();
    }

    private async Task OpenFocusModeAsync()
    {
        if (!ViewModel.IsWorkoutStarted || isFocusDialogOpen)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            FullSizeDesired = true,
            IsPrimaryButtonEnabled = false,
            IsSecondaryButtonEnabled = false
        };

        var focusPage = new FocusModeView(ViewModel, dialog);
        dialog.Content = focusPage;
        isFocusDialogOpen = true;
        try
        {
            await dialog.ShowAsync();
        }
        finally
        {
            isFocusDialogOpen = false;
        }
    }

    private void GoalRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string goal)
        {
            ViewModel.SelectedGoal = goal;
        }
    }

    private async void CreateCustomWorkout_Click(object sender, RoutedEventArgs e)
    {
        var createView = new CreateWorkoutView();
        createView.ViewModel.ClientId = ClientId;

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Create Custom Workout",
            Content = createView,
            PrimaryButtonText = "Save Routine",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            MinWidth = 700
        };

        dialog.PrimaryButtonClick += (d, args) =>
        {
            args.Cancel = true;
            createView.ViewModel.SaveWorkoutCommand.Execute(null);
        };

        createView.ViewModel.WorkoutSaved += () =>
        {
            dialog.Hide();
            ViewModel.LoadCustomWorkouts(ClientId);
        };

        await dialog.ShowAsync();
    }

    private void StartCustomWorkout_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WorkoutTemplate template)
        {
            ViewModel.SelectCustomWorkoutCommand.Execute(template);
        }
    }

    private void ApplyGoalsButton_Click(object sender, RoutedEventArgs e)
    {
        TargetGoalsButton.Flyout.Hide();
        ViewModel.ApplyTargetGoalsCommand.Execute(ClientId);
    }

    private void ConfirmDeloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Notification notification)
        {
            ViewModel.ConfirmDeloadCommand.Execute(notification);
        }
    }

    private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActiveWorkoutViewModel.IsWorkoutStarted) && ViewModel.IsWorkoutStarted)
        {
            await OpenFocusModeAsync();
        }
    }
}

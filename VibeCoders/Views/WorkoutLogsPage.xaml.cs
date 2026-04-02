using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class WorkoutLogsPage : Page
{
    public WorkoutLogsViewModel ViewModel { get; }

    public int ClientId { get; private set; }

    public WorkoutLogsPage()
    {
        ViewModel = App.GetService<WorkoutLogsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int clientId)
        {
            ClientId = clientId;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var clientId = ClientId != 0
            ? ClientId
            : (int)App.GetService<IUserSession>().CurrentClientId;
        ViewModel.LoadLogsCommand.Execute(clientId);
    }
}
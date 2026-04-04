using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class ClientProfileView : Page
{
    public ClientProfileViewModel ViewModel { get; }

    public ClientProfileView()
    {
        this.InitializeComponent();

        // Inject the repository
        var repository = new ClientDataRepository("Data Source=vibecoders.db");
        ViewModel = new ClientProfileViewModel(repository);
        this.DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is int clientId)
        {
            // Load client-specific exercises and nutrition
            ViewModel.LoadClientData(clientId);
        }
    }
}
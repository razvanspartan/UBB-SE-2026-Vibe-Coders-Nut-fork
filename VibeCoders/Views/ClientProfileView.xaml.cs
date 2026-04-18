using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class ClientProfileView : Page
{
    public ClientProfileViewModel ViewModel { get; }

    public ClientProfileView()
    {
        InitializeComponent();
        ViewModel = App.GetService<ClientProfileViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is int clientId)
        {
            ViewModel.LoadClientData(clientId);
        }
    }
}

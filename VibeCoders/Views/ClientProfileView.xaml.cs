namespace VibeCoders.Views;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.ViewModels;

public sealed partial class ClientProfileView : Page
{
    public ClientProfileViewModel ViewModel { get; }

    public ClientProfileView()
    {
        this.InitializeComponent();
        this.ViewModel = App.GetService<ClientProfileViewModel>();
        this.DataContext = this.ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
    {
        base.OnNavigatedTo(eventArgs);

        if (eventArgs.Parameter is int clientId)
        {
            this.ViewModel.LoadClientData(clientId);
        }
    }
}

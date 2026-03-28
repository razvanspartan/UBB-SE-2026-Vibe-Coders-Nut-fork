using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class ClientDashboardPage : Page
{
    public ClientDashboardViewModel ViewModel { get; }

    public ClientDashboardPage()
    {
        ViewModel = App.GetService<ClientDashboardViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadInitialAsync();
    }
}

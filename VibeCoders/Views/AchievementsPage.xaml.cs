using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class AchievementsPage : Page
{
    public AchievementsViewModel ViewModel { get; }
    public int ClientId { get; private set; }

    public AchievementsPage()
    {
        ViewModel = App.GetService<AchievementsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int clientId)
            ClientId = clientId;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadAchievementsCommand.Execute(ClientId);
    }
}

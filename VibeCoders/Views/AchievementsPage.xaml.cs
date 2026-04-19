namespace VibeCoders.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.ViewModels;

public sealed partial class AchievementsPage : Page
{
    public AchievementsViewModel ViewModel { get; }

    public int ClientId { get; private set; }

    public AchievementsPage()
    {
        this.ViewModel = App.GetService<AchievementsViewModel>();
        this.DataContext = this.ViewModel;
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
    {
        base.OnNavigatedTo(eventArgs);
        if (eventArgs.Parameter is int clientId)
        {
            this.ClientId = clientId;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs eventArgs)
    {
        this.ViewModel.LoadAchievementsCommand.Execute(this.ClientId);
    }
}

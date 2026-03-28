using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.Services;

namespace VibeCoders;

public sealed partial class MainWindow : Window
{
    private readonly NavigationService _navigationService;

    public MainWindow(NavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _navigationService.AttachFrame(ContentFrame);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            if (tag == "Dashboard")
            {
                _navigationService.NavigateToClientDashboard(requestRefresh: true);
            }
        }
    }
}

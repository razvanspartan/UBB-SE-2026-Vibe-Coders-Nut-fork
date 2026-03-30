using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

/// <summary>
/// Rank and level summary plus achievement showcase. Data loads on <see cref="Page.Loaded"/>
/// so the visual tree exists before hitting LocalDB and analytics.
/// </summary>
public sealed partial class RankShowcasePage : Page
{
    public RankShowcaseViewModel ViewModel { get; }

    public RankShowcasePage()
    {
        ViewModel = App.GetService<RankShowcaseViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync().ConfigureAwait(true);
    }
}

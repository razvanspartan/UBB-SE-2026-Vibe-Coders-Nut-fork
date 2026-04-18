using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class FocusModeView : Page
{
    private readonly ContentDialog hostDialog;

    public ActiveWorkoutViewModel ViewModel { get; }

    public FocusModeView(ActiveWorkoutViewModel vm, ContentDialog hostDialog)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = vm;
        this.hostDialog = hostDialog;
    }

    private void ExitButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        hostDialog.Hide();
    }
}

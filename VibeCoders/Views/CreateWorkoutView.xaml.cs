using Microsoft.UI.Xaml.Controls;
using VibeCoders.Models;
using VibeCoders.ViewModels;

namespace VibeCoders.Views
{
    public partial class CreateWorkoutView : UserControl
    {
        public CreateWorkoutViewModel ViewModel { get; }

        public CreateWorkoutView()
        {
            ViewModel = App.GetService<CreateWorkoutViewModel>();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void RemoveExercise_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TemplateExercise exercise)
            {
                ViewModel.Exercises.Remove(exercise);
            }
        }
    }
}

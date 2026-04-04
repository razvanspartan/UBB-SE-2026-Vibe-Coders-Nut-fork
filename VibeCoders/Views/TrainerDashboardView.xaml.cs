using Microsoft.UI.Xaml.Controls;
using System;
using VibeCoders.Services;
using VibeCoders.ViewModels;
using System.Collections.Generic;

namespace VibeCoders.Views
{
    public sealed partial class TrainerDashboardView : Page
    {
        public TrainerDashboardViewModel ViewModel { get; }

        private bool _isDialogOpen = false;

        public static string FormatWorkoutDate(DateTime Date)
        {

            return Date.ToString("MMM dd, yyyy");
        }

        public static string FormatLastWorkoutDate(List<VibeCoders.Models.WorkoutLog> logs)
        {
            
            if (logs != null && logs.Count > 0)
            {
                return $"Last Workout: {logs[0].Date.ToString("MMM dd, yyyy")}";
            }

            return "Last Workout: N/A";
        }

        public TrainerDashboardView()
        {
            var service = App.GetService<TrainerService>();
            var viewModel = App.GetService<TrainerDashboardViewModel>();
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }

        private async void OpenBuilderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_isDialogOpen) return;

            ViewModel.EditingTemplateId = 0;
            ViewModel.NewRoutineName = string.Empty;
            ViewModel.BuilderExercises.Clear();
            ViewModel.BuilderErrorText = string.Empty;

            WorkoutBuilderDialog.Title = "Assign New Routine";

            WorkoutBuilderDialog.XamlRoot = this.Content.XamlRoot;
            _isDialogOpen = true;
            await WorkoutBuilderDialog.ShowAsync();
            _isDialogOpen = false;
        }

        private void RemoveExercise_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var button = (Microsoft.UI.Xaml.Controls.Button)sender;
            var exercise = (VibeCoders.Models.TemplateExercise)button.DataContext;
            ViewModel.RemoveExerciseFromRoutine(exercise);
        }

        private void WorkoutBuilderDialog_PrimaryButtonClick(Microsoft.UI.Xaml.Controls.ContentDialog sender, Microsoft.UI.Xaml.Controls.ContentDialogButtonClickEventArgs args)
        {
            ViewModel.BuilderErrorText = string.Empty;

            if (string.IsNullOrWhiteSpace(ViewModel.NewRoutineName))
            {
                ViewModel.BuilderErrorText = "Routine Name cannot be empty.";
                args.Cancel = true;
                return;
            }

            if (ViewModel.BuilderExercises.Count == 0)
            {
                ViewModel.BuilderErrorText = "You must add at least one exercise to the routine.";
                args.Cancel = true;
                return;
            }

            var newTemplate = new VibeCoders.Models.WorkoutTemplate
            {
                Id = ViewModel.EditingTemplateId,
                ClientId = ViewModel.SelectedClient?.Id ?? 0,
                Name = ViewModel.NewRoutineName,
                Type = VibeCoders.Models.WorkoutType.TRAINER_ASSIGNED
            };

            foreach (var ex in ViewModel.BuilderExercises)
            {
                newTemplate.AddExercise(ex);
            }

            bool isSaved = ViewModel.SaveRoutine(newTemplate);

            if (isSaved)
            {
                Console.WriteLine("SUCCESS: Routine saved!");
                ViewModel.LoadAssignedWorkouts();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("FAILED: Could not save routine to database.");
                args.Cancel = true;
            }
        }

        private async void DeleteWorkout_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (_isDialogOpen) return;

            var button = (Microsoft.UI.Xaml.Controls.Button)sender;

            
            var workout = button.DataContext as VibeCoders.Models.WorkoutTemplate;

            if (workout == null)
            {
                System.Diagnostics.Debug.WriteLine("Error: Could not find workout data.");
                return;
            }

            ContentDialog confirmDelete = new ContentDialog
            {
                Title = "Delete Routine?",
                Content = $"Are you sure you want to remove '{workout.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = button.XamlRoot
            };

            _isDialogOpen = true;
            var result = await confirmDelete.ShowAsync();
            _isDialogOpen = false;

            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteRoutine(workout);
            }
        }

        private async void Card_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {

            if (_isDialogOpen || e.Handled) return;

            var grid = sender as Microsoft.UI.Xaml.Controls.Grid;
            var workout = grid?.DataContext as VibeCoders.Models.WorkoutTemplate;

            if (workout == null) return;

            System.Diagnostics.Debug.WriteLine($"---> TAPPED CARD: {workout.Name}");

            ViewModel.PrepareForEdit(workout);
            WorkoutBuilderDialog.Title = $"Edit Routine: {workout.Name}";
            WorkoutBuilderDialog.XamlRoot = this.Content.XamlRoot;

            _isDialogOpen = true;
            await WorkoutBuilderDialog.ShowAsync();
            _isDialogOpen = false;
        }

    }
}
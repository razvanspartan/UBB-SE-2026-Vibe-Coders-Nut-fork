namespace VibeCoders.Views
{
    using System;
    using System.Collections.Generic;
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.Services;
    using VibeCoders.ViewModels;

    public sealed partial class TrainerDashboardView : Page
    {
        public TrainerDashboardViewModel ViewModel { get; }

        private bool isDialogOpen = false;

        public static string FormatWorkoutDate(DateTime date)
        {
            return date.ToString("MMM dd, yyyy");
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

        private async void OpenBuilderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs eventArgs)
        {
            if (this.isDialogOpen)
            {
                return;
            }

            this.ViewModel.EditingTemplateId = 0;
            this.ViewModel.NewRoutineName = string.Empty;
            this.ViewModel.BuilderExercises.Clear();
            this.ViewModel.BuilderErrorText = string.Empty;

            this.WorkoutBuilderDialog.Title = "Assign New Routine";

            this.WorkoutBuilderDialog.XamlRoot = this.Content.XamlRoot;
            this.isDialogOpen = true;
            await this.WorkoutBuilderDialog.ShowAsync();
            this.isDialogOpen = false;
        }

        private void RemoveExercise_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs eventArgs)
        {
            var button = (Microsoft.UI.Xaml.Controls.Button)sender;
            var exercise = (VibeCoders.Models.TemplateExercise)button.DataContext;
            this.ViewModel.RemoveExerciseFromRoutine(exercise);
        }

        private void WorkoutBuilderDialog_PrimaryButtonClick(Microsoft.UI.Xaml.Controls.ContentDialog sender, Microsoft.UI.Xaml.Controls.ContentDialogButtonClickEventArgs args)
        {
            this.ViewModel.BuilderErrorText = string.Empty;

            if (string.IsNullOrWhiteSpace(this.ViewModel.NewRoutineName))
            {
                this.ViewModel.BuilderErrorText = "Routine Name cannot be empty.";
                args.Cancel = true;
                return;
            }

            if (this.ViewModel.BuilderExercises.Count == 0)
            {
                this.ViewModel.BuilderErrorText = "You must add at least one exercise to the routine.";
                args.Cancel = true;
                return;
            }

            var newTemplate = new VibeCoders.Models.WorkoutTemplate
            {
                Id = this.ViewModel.EditingTemplateId,
                ClientId = this.ViewModel.SelectedClient?.Id ?? 0,
                Name = this.ViewModel.NewRoutineName,
                Type = VibeCoders.Models.WorkoutType.TRAINER_ASSIGNED,
            };

            foreach (var exercise in this.ViewModel.BuilderExercises)
            {
                newTemplate.AddExercise(exercise);
            }

            bool isSaved = this.ViewModel.SaveRoutine(newTemplate);

            if (isSaved)
            {
                Console.WriteLine("SUCCESS: Routine saved!");
                this.ViewModel.LoadAssignedWorkouts();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("FAILED: Could not save routine to database.");
                args.Cancel = true;
            }
        }

        private async void DeleteWorkout_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            if (this.isDialogOpen)
            {
                return;
            }

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
                XamlRoot = button.XamlRoot,
            };

            this.isDialogOpen = true;
            var result = await confirmDelete.ShowAsync();
            this.isDialogOpen = false;

            if (result == ContentDialogResult.Primary)
            {
                this.ViewModel.DeleteRoutine(workout);
            }
        }

        private async void Card_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs eventArgs)
        {
            if (this.isDialogOpen || eventArgs.Handled)
            {
                return;
            }

            var grid = sender as Microsoft.UI.Xaml.Controls.Grid;
            var workout = grid?.DataContext as VibeCoders.Models.WorkoutTemplate;

            if (workout == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"---> TAPPED CARD: {workout.Name}");

            this.ViewModel.PrepareForEdit(workout);
            this.WorkoutBuilderDialog.Title = $"Edit Routine: {workout.Name}";
            this.WorkoutBuilderDialog.XamlRoot = this.Content.XamlRoot;

            this.isDialogOpen = true;
            await this.WorkoutBuilderDialog.ShowAsync();
            this.isDialogOpen = false;
        }
    }
}

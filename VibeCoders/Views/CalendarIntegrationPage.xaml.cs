using Microsoft.UI.Xaml.Controls;

namespace VibeCoders.Views
{
    public sealed partial class CalendarIntegrationPage : Page
    {
        public CalendarIntegrationPage()
        {
            this.InitializeComponent();
        }

        private void GenerateCalendarButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // Validate workout selection
            if (WorkoutComboBox.SelectedIndex == -1)
            {
                ShowError("Please select a workout from the dropdown.");
                return;
            }

            // Validate duration input
            string durationInput = DurationWeeksTextBox.Text.Trim();
            if (string.IsNullOrEmpty(durationInput))
            {
                ShowError("Please enter the number of weeks (1-52).");
                return;
            }

            if (!int.TryParse(durationInput, out int weeks))
            {
                ShowError("Duration must be a number between 1 and 52.");
                return;
            }

            if (weeks < 1 || weeks > 52)
            {
                ShowError("Duration must be between 1 and 52 weeks.");
                return;
            }

            // Validate at least one training day selected
            if (!IsAnyDaySelected())
            {
                ShowError("Please select at least one training day.");
                return;
            }

            // TODO: Generate calendar with validated data
            string selectedWorkout = "";
            if (WorkoutComboBox.SelectedItem is ComboBoxItem item)
            {
                selectedWorkout = item.Content?.ToString() ?? "Unknown Workout";
            }
            ShowSuccess($"Calendar will be generated for {selectedWorkout} - {weeks} weeks");
        }

        private bool IsAnyDaySelected()
        {
            return DayMonday.IsChecked == true ||
                   DayTuesday.IsChecked == true ||
                   DayWednesday.IsChecked == true ||
                   DayThursday.IsChecked == true ||
                   DayFriday.IsChecked == true ||
                   DaySaturday.IsChecked == true ||
                   DaySunday.IsChecked == true;
        }

        private void ShowError(string message)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = "Validation Error";
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }

        private void ShowSuccess(string message)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.Title = "Success";
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }
    }
}

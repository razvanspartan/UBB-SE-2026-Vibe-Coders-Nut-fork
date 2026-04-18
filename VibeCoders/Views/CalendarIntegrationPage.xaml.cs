using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;
using Windows.Storage.Pickers;

namespace VibeCoders.Views
{
    public sealed partial class CalendarIntegrationPage : Page
    {
        private CalendarIntegrationViewModel? viewModel;

        public CalendarIntegrationPage()
        {
            this.InitializeComponent();

            viewModel = App.GetService<CalendarIntegrationViewModel>();
            this.DataContext = viewModel;

            this.Loaded += async (s, e) =>
            {
                GenerateCalendarButton.Click += GenerateCalendarButton_Click;

                if (viewModel != null)
                {
                    await viewModel.EnsureWorkoutsLoadedAsync();
                }
            };
        }

        private async void GenerateCalendarButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null)
            {
                return;
            }

            try
            {
                GenerateCalendarButton.IsEnabled = false;

                string? validationError = viewModel.ValidateInput();
                if (validationError != null)
                {
                    ShowError(validationError);
                    return;
                }

                var icsContent = await viewModel.GenerateCalendarAsync();

                if (string.IsNullOrEmpty(icsContent))
                {
                    ShowError("Failed to generate calendar file. Please try again.");
                    return;
                }

                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                savePicker.FileTypeChoices.Add("iCalendar", new System.Collections.Generic.List<string> { ".ics" });

                var window = (Application.Current as App)?.Window;
                if (window == null)
                {
                    ShowError("Unable to access app window for save dialog.");
                    return;
                }

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                if (hWnd == IntPtr.Zero)
                {
                    ShowError("Unable to initialize save dialog window handle.");
                    return;
                }

                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                var file = await savePicker.PickSaveFileAsync();

                if (file == null)
                {
                    return;
                }

                await Windows.Storage.FileIO.WriteTextAsync(file, icsContent);

                ShowSuccess($"Calendar file '{file.Name}' saved successfully! You can now import it into your calendar application.");
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                if (ex is COMException)
                {
                    var fallbackPath = await SaveToDownloadsFallbackAsync();
                    if (!string.IsNullOrWhiteSpace(fallbackPath))
                    {
                        ShowSuccess($"Save dialog unavailable. Calendar saved to: {fallbackPath}");
                    }
                    else
                    {
                        ShowError("Error saving calendar file: could not open the save dialog.");
                    }
                }
                else
                {
                    ShowError($"Error saving calendar file: {ex.Message}");
                }
            }
            finally
            {
                GenerateCalendarButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = "Error";
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

        private async Task<string?> SaveToDownloadsFallbackAsync()
        {
            if (viewModel == null || string.IsNullOrEmpty(viewModel.GeneratedIcsContent))
            {
                return null;
            }

            try
            {
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                Directory.CreateDirectory(downloadsPath);

                var safeWorkoutName = (viewModel.SelectedWorkout?.Name ?? "Workout")
                    .Replace(" ", "-")
                    .Replace("/", "-")
                    .Replace("\\", "-");

                var fileName = $"{safeWorkoutName}-{DateTime.Now:yyyyMMdd-HHmmss}.ics";
                var fullPath = Path.Combine(downloadsPath, fileName);

                await File.WriteAllTextAsync(fullPath, viewModel.GeneratedIcsContent);
                return fullPath;
            }
            catch
            {
                return null;
            }
        }
    }
}


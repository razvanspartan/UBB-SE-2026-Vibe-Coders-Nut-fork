using System;
using System.Runtime.InteropServices;
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
                    calendarIntegrationViewModel.SetErrorStatus(calendarGenerationResult.Message);
                    return;
                }

                await Windows.Storage.FileIO.WriteTextAsync(file, icsContent);

                ShowSuccess($"Calendar file '{file.Name}' saved successfully! You can now import it into your calendar application.");
                string generatedCalendarContent = calendarGenerationResult.GeneratedCalendarContent;
                await SaveGeneratedCalendarContentWithPickerAsync(
                    generatedCalendarContent,
                    calendarIntegrationViewModel);
            }
            catch (InvalidOperationException ex)
            {
                calendarIntegrationViewModel.SetErrorStatus(ex.Message);
            }
            catch (Exception ex)
            {
                await HandleCalendarSaveExceptionAsync(ex, calendarIntegrationViewModel);
            }
            finally
            {
                GenerateCalendarButton.IsEnabled = true;
            }
        }

        private async Task SaveGeneratedCalendarContentWithPickerAsync(
            string generatedCalendarContent,
            CalendarIntegrationViewModel calendarIntegrationViewModel)
        {
            FileSavePicker calendarFileSavePicker = CreateCalendarFileSavePicker();

            if (!TryGetApplicationWindowHandle(
                    calendarIntegrationViewModel,
                    out IntPtr applicationWindowHandle))
            {
                return;
            }

            WinRT.Interop.InitializeWithWindow.Initialize(calendarFileSavePicker, applicationWindowHandle);

            var selectedStorageFile = await calendarFileSavePicker.PickSaveFileAsync();
            if (selectedStorageFile == null)
            {
                return;
            }

            await Windows.Storage.FileIO.WriteTextAsync(selectedStorageFile, generatedCalendarContent);
            calendarIntegrationViewModel.SetSuccessStatus(
                $"Calendar file '{selectedStorageFile.Name}' saved successfully! You can now import it into your calendar application.");
        }

        private static FileSavePicker CreateCalendarFileSavePicker()
        {
            FileSavePicker calendarFileSavePicker = new FileSavePicker();
            calendarFileSavePicker.SuggestedStartLocation = PickerLocationId.Downloads;
            calendarFileSavePicker.FileTypeChoices.Add(
                "iCalendar",
                new System.Collections.Generic.List<string> { ".ics" });
            return calendarFileSavePicker;
        }

        private static bool TryGetApplicationWindowHandle(
            CalendarIntegrationViewModel calendarIntegrationViewModel,
            out IntPtr applicationWindowHandle)
        {
            var applicationWindow = (Application.Current as App)?._window;
            if (applicationWindow == null)
            if (viewModel == null || string.IsNullOrEmpty(viewModel.GeneratedIcsContent))
            {
                calendarIntegrationViewModel.SetErrorStatus("Unable to access app window for save dialog.");
                applicationWindowHandle = IntPtr.Zero;
                return false;
            }

            applicationWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(applicationWindow);
            if (applicationWindowHandle == IntPtr.Zero)
            {
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                Directory.CreateDirectory(downloadsPath);

                var safeWorkoutName = (viewModel.SelectedWorkout?.Name ?? "Workout")
                    .Replace(" ", "-")
                    .Replace("/", "-")
                    .Replace("\\", "-");
                calendarIntegrationViewModel.SetErrorStatus("Unable to initialize save dialog window handle.");
                return false;
            }

            return true;
        }

        private static async Task HandleCalendarSaveExceptionAsync(
            Exception calendarSaveException,
            CalendarIntegrationViewModel calendarIntegrationViewModel)
        {
            if (calendarSaveException is COMException)
            {
                var fallbackCalendarPath = await calendarIntegrationViewModel.SaveGeneratedCalendarToDownloadsFallbackAsync();
                if (!string.IsNullOrWhiteSpace(fallbackCalendarPath))
                {
                    calendarIntegrationViewModel.SetSuccessStatus($"Save dialog unavailable. Calendar saved to: {fallbackCalendarPath}");
                }
                else
                {
                    calendarIntegrationViewModel.SetErrorStatus("Error saving calendar file: could not open the save dialog.");
                }

                return;
            }

            calendarIntegrationViewModel.SetErrorStatus($"Error saving calendar file: {calendarSaveException.Message}");
        }
    }
}


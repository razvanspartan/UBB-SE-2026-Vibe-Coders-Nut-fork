namespace VibeCoders.Views
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.ViewModels;
    using Windows.Storage.Pickers;

    public sealed partial class CalendarIntegrationPage : Page
    {
        private CalendarIntegrationViewModel? viewModel;

        public CalendarIntegrationPage()
        {
            this.InitializeComponent();

            this.viewModel = App.GetService<CalendarIntegrationViewModel>();
            this.DataContext = this.viewModel;

            this.GenerateCalendarButton.Click += this.GenerateCalendarButton_Click;
            this.Loaded += this.CalendarIntegrationPage_Loaded;
        }

        private async void CalendarIntegrationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.viewModel != null)
            {
                await this.viewModel.EnsureWorkoutsLoadedAsync();
            }
        }

        private async void GenerateCalendarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.viewModel == null)
            {
                return;
            }

            await this.ExecuteCalendarGenerationAndSaveFlowAsync(this.viewModel);
        }

        private async Task ExecuteCalendarGenerationAndSaveFlowAsync(CalendarIntegrationViewModel calendarIntegrationViewModel)
        {
            try
            {
                this.GenerateCalendarButton.IsEnabled = false;
                calendarIntegrationViewModel.ClearStatus();

                CalendarIntegrationViewModel.CalendarGenerationResult calendarGenerationResult =
                    await calendarIntegrationViewModel.GenerateCalendarForExportAsync();

                if (!calendarGenerationResult.IsSuccessful)
                {
                    calendarIntegrationViewModel.SetErrorStatus(calendarGenerationResult.Message);
                    return;
                }

                string generatedCalendarContent = calendarGenerationResult.GeneratedCalendarContent;
                await this.SaveGeneratedCalendarContentWithPickerAsync(
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
                this.GenerateCalendarButton.IsEnabled = true;
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
            var applicationWindow = (Application.Current as App)?.Window;
            if (applicationWindow == null)
            {
                calendarIntegrationViewModel.SetErrorStatus("Unable to access app window for save dialog.");
                applicationWindowHandle = IntPtr.Zero;
                return false;
            }

            applicationWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(applicationWindow);
            if (applicationWindowHandle == IntPtr.Zero)
            {
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

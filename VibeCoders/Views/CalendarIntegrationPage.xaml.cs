#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1601 // Partial elements should be documented

namespace VibeCoders.Views
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using VibeCoders.ViewModels;
    using Windows.Storage.Pickers;

    public sealed partial class CalendarIntegrationPage : Page
    {
        private const string IcsExtension = ".ics";
        private const string CalendarFormatName = "iCalendar";
        private const string DownloadsDirectoryName = "Downloads";
        private const string DefaultFileNamePrefix = "Workout";
        private const string ErrorTitle = "Error";
        private const string SuccessTitle = "Success";
        private const string FileNameDateTimeFormat = "yyyyMMdd-HHmmss";

        private readonly CalendarIntegrationViewModel? viewModel;

        public CalendarIntegrationPage()
        {
            this.InitializeComponent();

            this.viewModel = App.GetService<CalendarIntegrationViewModel>();
            this.DataContext = this.viewModel;

            this.Loaded += this.OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.GenerateCalendarButton.Click += this.GenerateCalendarButton_Click;

            if (this.viewModel != null)
            {
                await this.viewModel.EnsureWorkoutsLoadedAsync();
            }
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Event handler naming convention matches XAML.")]
        private async void GenerateCalendarButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            if (this.viewModel == null)
            {
                return;
            }

            try
            {
                this.GenerateCalendarButton.IsEnabled = false;

                string? validationError = this.viewModel.ValidateInput();
                if (validationError != null)
                {
                    this.ShowError(validationError);
                    return;
                }

                string calendarFileContent = await this.viewModel.GenerateCalendarAsync();

                if (string.IsNullOrEmpty(calendarFileContent))
                {
                    this.ShowError("Failed to generate calendar file. Please try again.");
                    return;
                }

                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                savePicker.FileTypeChoices.Add(CalendarIntegrationPage.CalendarFormatName, new List<string> { CalendarIntegrationPage.IcsExtension });

                Window? applicationWindow = (Application.Current as App)?._window;
                if (applicationWindow == null)
                {
                    this.ShowError("Unable to access app window for save dialog.");
                    return;
                }

                IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(applicationWindow);
                if (windowHandle == IntPtr.Zero)
                {
                    this.ShowError("Unable to initialize save dialog window handle.");
                    return;
                }

                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

                Windows.Storage.StorageFile savedFile = await savePicker.PickSaveFileAsync();

                if (savedFile == null)
                {
                    return;
                }

                await Windows.Storage.FileIO.WriteTextAsync(savedFile, calendarFileContent);

                this.ShowSuccess($"Calendar file '{savedFile.Name}' saved successfully! You can now import it into your calendar application.");
            }
            catch (InvalidOperationException invalidOperationException)
            {
                this.ShowError(invalidOperationException.Message);
            }
            catch (Exception generalException)
            {
                if (generalException is COMException)
                {
                    string? fallbackPath = await this.SaveToDownloadsFallbackAsync();
                    if (!string.IsNullOrWhiteSpace(fallbackPath))
                    {
                        this.ShowSuccess($"Save dialog unavailable. Calendar saved to: {fallbackPath}");
                    }
                    else
                    {
                        this.ShowError("Error saving calendar file: could not open the save dialog.");
                    }
                }
                else
                {
                    this.ShowError($"Error saving calendar file: {generalException.Message}");
                }
            }
            finally
            {
                this.GenerateCalendarButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            this.StatusInfoBar.Severity = InfoBarSeverity.Error;
            this.StatusInfoBar.Title = CalendarIntegrationPage.ErrorTitle;
            this.StatusInfoBar.Message = message;
            this.StatusInfoBar.IsOpen = true;
        }

        private void ShowSuccess(string message)
        {
            this.StatusInfoBar.Severity = InfoBarSeverity.Success;
            this.StatusInfoBar.Title = CalendarIntegrationPage.SuccessTitle;
            this.StatusInfoBar.Message = message;
            this.StatusInfoBar.IsOpen = true;
        }

        private async Task<string?> SaveToDownloadsFallbackAsync()
        {
            if (this.viewModel == null || string.IsNullOrEmpty(this.viewModel.GeneratedCalendarFileContent))
            {
                return null;
            }

            try
            {
                string downloadsFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    CalendarIntegrationPage.DownloadsDirectoryName);

                Directory.CreateDirectory(downloadsFolderPath);

                string sanitizedWorkoutName = (this.viewModel.SelectedWorkout?.Name ?? CalendarIntegrationPage.DefaultFileNamePrefix)
                    .Replace(" ", "-")
                    .Replace("/", "-")
                    .Replace("\\", "-");

                string generatedFileName = $"{sanitizedWorkoutName}-{DateTime.Now:yyyyMMdd-HHmmss}{CalendarIntegrationPage.IcsExtension}";
                string fullStoragePath = Path.Combine(downloadsFolderPath, generatedFileName);

                await File.WriteAllTextAsync(fullStoragePath, this.viewModel.GeneratedCalendarFileContent);
                return fullStoragePath;
            }
            catch
            {
                return null;
            }
        }
    }
}
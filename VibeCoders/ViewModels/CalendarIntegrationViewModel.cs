using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public partial class DaySelectionItem : ObservableObject
    {
        public int DayOfWeekIndex { get; }

        public string DayName { get; }

        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        public DaySelectionItem(int dayOfWeekIndex, string dayName, bool initialSelection = false)
        {
            DayOfWeekIndex = dayOfWeekIndex;
            DayName = dayName;
            IsSelected = initialSelection;
        }
    }

    public partial class CalendarIntegrationViewModel : ObservableObject
    {
        private readonly ICalendarWorkoutCatalogService _workoutCatalogService;
        private readonly ICalendarExportService _calendarExportService;
        private readonly IUserSession _userSession;

        [ObservableProperty]
        public partial ObservableCollection<WorkoutTemplate> AvailableWorkouts { get; set; } = new();

        [ObservableProperty]
        public partial WorkoutTemplate? SelectedWorkout { get; set; }

        [ObservableProperty]
        public partial int DurationWeeks { get; set; } = 4;

        [ObservableProperty]
        public partial ObservableCollection<DaySelectionItem> SelectedDays { get; set; } = new();

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial string GeneratedIcsContent { get; set; } = string.Empty;

        [ObservableProperty]
        public partial InfoBarSeverity StatusSeverity { get; set; }

        [ObservableProperty]
        public partial string StatusTitle { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsStatusOpen { get; set; }

        public CalendarIntegrationViewModel(
            ICalendarWorkoutCatalogService workoutCatalogService,
            ICalendarExportService calendarExportService,
            IUserSession userSession)
        {
            _workoutCatalogService = workoutCatalogService ?? throw new ArgumentNullException(nameof(workoutCatalogService));
            _calendarExportService = calendarExportService ?? throw new ArgumentNullException(nameof(calendarExportService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            InitializeDaySelection();

            // Populate immediately so UI is responsive even if DB is unavailable.
            var clientId = (int)_userSession.CurrentClientId;
            SetAvailableWorkouts(_workoutCatalogService.GetFallbackWorkouts(clientId));

            _ = LoadAvailableWorkoutsAsync();
        }

        private void InitializeDaySelection()
        {
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var defaultSelections = new[] { false, true, true, true, true, true, false };

            SelectedDays.Clear();
            for (int i = 0; i < 7; i++)
            {
                SelectedDays.Add(new DaySelectionItem(i, dayNames[i], defaultSelections[i]));
            }
        }

        public async Task LoadAvailableWorkoutsAsync()
        {
            try
            {
                IsLoading = true;

                var clientId = (int)_userSession.CurrentClientId;
                var workouts = await _workoutCatalogService
                    .GetAvailableWorkoutsAsync(clientId, TimeSpan.FromMilliseconds(1500));
                SetAvailableWorkouts(workouts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {ex.Message}");
                var clientId = (int)_userSession.CurrentClientId;
                SetAvailableWorkouts(_workoutCatalogService.GetFallbackWorkouts(clientId));
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task EnsureWorkoutsLoadedAsync()
        {
            if (IsLoading || AvailableWorkouts.Count > 0)
            {
                return;
            }

            await LoadAvailableWorkoutsAsync();
        }

        private void SetAvailableWorkouts(IEnumerable<WorkoutTemplate> workouts)
        {
            AvailableWorkouts.Clear();
            foreach (var workout in workouts)
            {
                AvailableWorkouts.Add(workout);
            }

            if (AvailableWorkouts.Count > 0)
            {
                SelectedWorkout = AvailableWorkouts[0];
            }
        }

        public int[] GetSelectedDaysOfWeek()
        {
            var selectedDayIndexes = new List<int>();

            for (int selectedDayIndex = 0; selectedDayIndex < SelectedDays.Count; selectedDayIndex++)
            {
                var selectedDayItem = SelectedDays[selectedDayIndex];
                if (selectedDayItem.IsSelected)
                {
                    selectedDayIndexes.Add(selectedDayItem.DayOfWeekIndex);
                }
            }

            return selectedDayIndexes.ToArray();
        }

        public string? ValidateInput()
        {
            if (SelectedWorkout == null)
            {
                return "Please select a workout from the dropdown.";
            }

            if (DurationWeeks < 1 || DurationWeeks > 52)
            {
                return "Duration must be between 1 and 52 weeks.";
            }

            var selectedDaysArray = GetSelectedDaysOfWeek();
            if (selectedDaysArray.Length == 0)
            {
                return "Please select at least one training day.";
            }

            return null;
        }

        public Task<string> GenerateCalendarAsync()
        {
            var validationErrorMessage = ValidateInput();
            if (validationErrorMessage != null)
            {
                throw new InvalidOperationException(validationErrorMessage);
            }

            if (SelectedWorkout == null)
            {
                throw new InvalidOperationException("No workout selected.");
            }

            var selectedDayIndexes = GetSelectedDaysOfWeek();
            string generatedCalendarContent = _calendarExportService.GenerateCalendar(
                SelectedWorkout,
                DurationWeeks,
                selectedDayIndexes);

            GeneratedIcsContent = generatedCalendarContent;
            return Task.FromResult(generatedCalendarContent);
        }

        public async Task<CalendarGenerationResult> GenerateCalendarForExportAsync()
        {
            try
            {
                string generatedCalendarContent = await GenerateCalendarAsync();
                if (string.IsNullOrEmpty(generatedCalendarContent))
                {
                    return CalendarGenerationResult.CreateFailure("Failed to generate calendar file. Please try again.");
                }

                return CalendarGenerationResult.CreateSuccess(generatedCalendarContent);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                return CalendarGenerationResult.CreateFailure(invalidOperationException.Message);
            }
        }

        public Task<string?> SaveGeneratedCalendarToDownloadsFallbackAsync()
        {
            string selectedWorkoutName = SelectedWorkout?.Name ?? "Workout";
            return _calendarExportService.SaveCalendarToDownloadsAsync(GeneratedIcsContent, selectedWorkoutName);
        }

        public void SetErrorStatus(string errorMessage)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusTitle = "Error";
            StatusMessage = errorMessage;
            IsStatusOpen = true;
        }

        public void SetSuccessStatus(string successMessage)
        {
            StatusSeverity = InfoBarSeverity.Success;
            StatusTitle = "Success";
            StatusMessage = successMessage;
            IsStatusOpen = true;
        }

        public void ClearStatus()
        {
            StatusTitle = string.Empty;
            StatusMessage = string.Empty;
            IsStatusOpen = false;
        }

        public void ToggleDaySelection(int dayOfWeek)
        {
            for (int selectedDayIndex = 0; selectedDayIndex < SelectedDays.Count; selectedDayIndex++)
            {
                DaySelectionItem daySelectionItem = SelectedDays[selectedDayIndex];
                if (daySelectionItem.DayOfWeekIndex == dayOfWeek)
                {
                    daySelectionItem.IsSelected = !daySelectionItem.IsSelected;
                    return;
                }
            }
        }

        public sealed class CalendarGenerationResult
        {
            public bool IsSuccessful { get; init; }

            public string Message { get; init; } = string.Empty;

            public string GeneratedCalendarContent { get; init; } = string.Empty;

            public static CalendarGenerationResult CreateSuccess(string generatedCalendarContent)
            {
                return new CalendarGenerationResult
                {
                    IsSuccessful = true,
                    GeneratedCalendarContent = generatedCalendarContent,
                };
            }

            public static CalendarGenerationResult CreateFailure(string failureMessage)
            {
                return new CalendarGenerationResult
                {
                    IsSuccessful = false,
                    Message = failureMessage,
                };
            }
        }
    }
}
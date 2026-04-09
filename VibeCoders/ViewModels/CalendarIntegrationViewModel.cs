#pragma warning disable MVVMTK0045

namespace VibeCoders.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using VibeCoders.Models;
    using VibeCoders.Services;

    /// <summary>
    /// Represents a day of the week for selection in the calendar integration view.
    /// </summary>
    public class DaySelectionItem : ObservableObject
    {
        private bool isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="DaySelectionItem"/> class.
        /// </summary>
        /// <param name="dayOfWeekIndex">The zero-based index of the day of the week (0=Sunday).</param>
        /// <param name="dayName">The display name of the day.</param>
        /// <param name="initialSelection">The initial selection state.</param>
        public DaySelectionItem(int dayOfWeekIndex, string dayName, bool initialSelection = false)
        {
            this.DayOfWeekIndex = dayOfWeekIndex;
            this.DayName = dayName;
            this.isSelected = initialSelection;
        }

        /// <summary>
        /// Gets the zero-based index of the day of the week.
        /// </summary>
        public int DayOfWeekIndex { get; }

        /// <summary>
        /// Gets the display name of the day.
        /// </summary>
        public string DayName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the day is selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }
    }

    /// <summary>
    /// ViewModel for the calendar integration page, managing workout selection and export.
    /// </summary>
    public class CalendarIntegrationViewModel : ObservableObject
    {
        private const int TotalDaysInWeek = 7;
        private const int DatabaseTimeoutMilliseconds = 1500;
        private const int MinimumDurationWeeks = 1;
        private const int MaximumDurationWeeks = 52;
        private const int DefaultDurationWeeks = 4;

        private readonly IDataStorage dataStorage;
        private readonly ICalendarExportService calendarExportService;
        private readonly IUserSession userSession;

        private ObservableCollection<WorkoutTemplate> availableWorkouts = new ObservableCollection<WorkoutTemplate>();
        private WorkoutTemplate? selectedWorkout;
        private int durationWeeks = CalendarIntegrationViewModel.DefaultDurationWeeks;
        private ObservableCollection<DaySelectionItem> selectedDays = new ObservableCollection<DaySelectionItem>();
        private bool isLoading;
        private string generatedCalendarFileContent = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarIntegrationViewModel"/> class.
        /// </summary>
        /// <param name="dataStorage">The data storage service.</param>
        /// <param name="calendarExportService">The calendar export service.</param>
        /// <param name="userSession">The user session service.</param>
        public CalendarIntegrationViewModel(
            IDataStorage dataStorage,
            ICalendarExportService calendarExportService,
            IUserSession userSession)
        {
            this.dataStorage = dataStorage ?? throw new ArgumentNullException(nameof(dataStorage));
            this.calendarExportService = calendarExportService ?? throw new ArgumentNullException(nameof(calendarExportService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            this.InitializeDaySelection();

            int clientId = (int)this.userSession.CurrentClientId;
            this.LoadFallbackWorkouts(clientId);

            _ = this.LoadAvailableWorkoutsAsync();
        }

        /// <summary>
        /// Gets or sets the collection of available workout templates.
        /// </summary>
        public ObservableCollection<WorkoutTemplate> AvailableWorkouts
        {
            get => this.availableWorkouts;
            set => this.SetProperty(ref this.availableWorkouts, value);
        }

        /// <summary>
        /// Gets or sets the currently selected workout template.
        /// </summary>
        public WorkoutTemplate? SelectedWorkout
        {
            get => this.selectedWorkout;
            set => this.SetProperty(ref this.selectedWorkout, value);
        }

        /// <summary>
        /// Gets or sets the duration in weeks for the generated calendar.
        /// </summary>
        public int DurationWeeks
        {
            get => this.durationWeeks;
            set => this.SetProperty(ref this.durationWeeks, value);
        }

        /// <summary>
        /// Gets or sets the collection of day selection items.
        /// </summary>
        public ObservableCollection<DaySelectionItem> SelectedDays
        {
            get => this.selectedDays;
            set => this.SetProperty(ref this.selectedDays, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether data is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get => this.isLoading;
            set => this.SetProperty(ref this.isLoading, value);
        }

        /// <summary>
        /// Gets or sets the generated iCalendar content.
        /// </summary>
        public string GeneratedCalendarFileContent
        {
            get => this.generatedCalendarFileContent;
            set => this.SetProperty(ref this.generatedCalendarFileContent, value);
        }

        /// <summary>
        /// Loads available workouts from the data storage asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadAvailableWorkoutsAsync()
        {
            try
            {
                this.IsLoading = true;

                int clientId = (int)this.userSession.CurrentClientId;

                var databaseLoadTask = Task.Run(() => this.dataStorage.GetAvailableWorkouts(clientId));
                var completedTask = await Task.WhenAny(databaseLoadTask, Task.Delay(CalendarIntegrationViewModel.DatabaseTimeoutMilliseconds));

                if (completedTask != databaseLoadTask)
                {
                    return;
                }

                var workouts = await databaseLoadTask;

                this.AvailableWorkouts.Clear();
                foreach (var workout in workouts)
                {
                    this.AvailableWorkouts.Add(workout);
                }

                if (this.AvailableWorkouts.Count == 0)
                {
                    this.LoadFallbackWorkouts(clientId);
                    return;
                }

                int firstElementIndex = 0;
                if (this.SelectedWorkout == null && this.AvailableWorkouts.Count > 0)
                {
                    this.SelectedWorkout = this.AvailableWorkouts[firstElementIndex];
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {exception.Message}");
                int clientId = (int)this.userSession.CurrentClientId;
                this.LoadFallbackWorkouts(clientId);
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        /// <summary>
        /// Ensures that workouts are loaded, triggering a load if necessary.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureWorkoutsLoadedAsync()
        {
            if (this.IsLoading || this.AvailableWorkouts.Count > 0)
            {
                return;
            }

            await this.LoadAvailableWorkoutsAsync();
        }

        /// <summary>
        /// Gets the indices of the currently selected days of the week.
        /// </summary>
        /// <returns>An array of day indices.</returns>
        public int[] GetSelectedDaysOfWeek()
        {
            return this.SelectedDays
                .Where(daySelectionItem => daySelectionItem.IsSelected)
                .Select(daySelectionItem => daySelectionItem.DayOfWeekIndex)
                .ToArray();
        }

        /// <summary>
        /// Validates the current input state of the ViewModel.
        /// </summary>
        /// <returns>An error message if validation fails; otherwise, null.</returns>
        public string? ValidateInput()
        {
            if (this.SelectedWorkout == null)
            {
                return "Please select a workout from the dropdown.";
            }

            if (this.DurationWeeks < CalendarIntegrationViewModel.MinimumDurationWeeks || this.DurationWeeks > CalendarIntegrationViewModel.MaximumDurationWeeks)
            {
                return "Duration must be between 1 and 52 weeks.";
            }

            var selectedDaysArray = this.GetSelectedDaysOfWeek();
            if (selectedDaysArray.Length == 0)
            {
                return "Please select at least one training day.";
            }

            return null;
        }

        /// <summary>
        /// Generates the iCalendar file content based on the current selections.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing the generated content.</returns>
        public async Task<string> GenerateCalendarAsync()
        {
            var calendarFileContent = await Task.Run(() =>
            {
                var validationError = this.ValidateInput();
                if (validationError != null)
                {
                    throw new InvalidOperationException(validationError);
                }

                if (this.SelectedWorkout == null)
                {
                    throw new InvalidOperationException("No workout selected.");
                }

                var selectedDaysArray = this.GetSelectedDaysOfWeek();
                return this.calendarExportService.GenerateCalendar(
                    this.SelectedWorkout,
                    this.DurationWeeks,
                    selectedDaysArray);
            });

            this.GeneratedCalendarFileContent = calendarFileContent;
            return calendarFileContent;
        }

        /// <summary>
        /// Toggles the selection state of a specific day of the week.
        /// </summary>
        /// <param name="dayOfWeekIndex">The index of the day to toggle.</param>
        public void ToggleDaySelection(int dayOfWeekIndex)
        {
            var daySelectionItem = this.SelectedDays.FirstOrDefault(item => item.DayOfWeekIndex == dayOfWeekIndex);
            if (daySelectionItem != null)
            {
                daySelectionItem.IsSelected = !daySelectionItem.IsSelected;
            }
        }

        private static List<WorkoutTemplate> CreateFallbackWorkouts(int clientId)
        {
            int fallbackIdOne = -1;
            int fallbackIdTwo = -2;
            int fallbackIdThree = -3;
            int fallbackIdFour = -4;
            int defaultTargetSets = 4;
            int defaultTargetReps = 6;

            var fullBodyStrength = new WorkoutTemplate
            {
                Id = fallbackIdOne,
                ClientId = clientId,
                Name = "Fallback - Full Body Strength",
                Type = WorkoutType.PREBUILT
            };
            fullBodyStrength.AddExercise(new TemplateExercise { Id = -101, WorkoutTemplateId = fallbackIdOne, Name = "Back Squat", MuscleGroup = MuscleGroup.LEGS, TargetSets = defaultTargetSets, TargetReps = defaultTargetReps });
            fullBodyStrength.AddExercise(new TemplateExercise { Id = -102, WorkoutTemplateId = fallbackIdOne, Name = "Bench Press", MuscleGroup = MuscleGroup.CHEST, TargetSets = defaultTargetSets, TargetReps = defaultTargetReps });
            fullBodyStrength.AddExercise(new TemplateExercise { Id = -103, WorkoutTemplateId = fallbackIdOne, Name = "Barbell Row", MuscleGroup = MuscleGroup.BACK, TargetSets = defaultTargetSets, TargetReps = 8 });

            var highIntensityIntervalTraining = new WorkoutTemplate
            {
                Id = fallbackIdTwo,
                ClientId = clientId,
                Name = "Fallback - HIIT Conditioning",
                Type = WorkoutType.PREBUILT
            };
            highIntensityIntervalTraining.AddExercise(new TemplateExercise { Id = -201, WorkoutTemplateId = fallbackIdTwo, Name = "Burpees", MuscleGroup = MuscleGroup.CORE, TargetSets = defaultTargetSets, TargetReps = 12 });
            highIntensityIntervalTraining.AddExercise(new TemplateExercise { Id = -202, WorkoutTemplateId = fallbackIdTwo, Name = "Jump Squats", MuscleGroup = MuscleGroup.LEGS, TargetSets = defaultTargetSets, TargetReps = 15 });
            highIntensityIntervalTraining.AddExercise(new TemplateExercise { Id = -203, WorkoutTemplateId = fallbackIdTwo, Name = "Mountain Climbers", MuscleGroup = MuscleGroup.CORE, TargetSets = defaultTargetSets, TargetReps = 20 });

            var pushPull = new WorkoutTemplate
            {
                Id = fallbackIdThree,
                ClientId = clientId,
                Name = "Fallback - Push Pull Split",
                Type = WorkoutType.PREBUILT
            };
            pushPull.AddExercise(new TemplateExercise { Id = -301, WorkoutTemplateId = fallbackIdThree, Name = "Overhead Press", MuscleGroup = MuscleGroup.SHOULDERS, TargetSets = defaultTargetSets, TargetReps = 8 });
            pushPull.AddExercise(new TemplateExercise { Id = -302, WorkoutTemplateId = fallbackIdThree, Name = "Pull-Ups", MuscleGroup = MuscleGroup.BACK, TargetSets = defaultTargetSets, TargetReps = 8 });
            pushPull.AddExercise(new TemplateExercise { Id = -303, WorkoutTemplateId = fallbackIdThree, Name = "Dumbbell Curl", MuscleGroup = MuscleGroup.ARMS, TargetSets = 3, TargetReps = 12 });

            var coreMobility = new WorkoutTemplate
            {
                Id = fallbackIdFour,
                ClientId = clientId,
                Name = "Fallback - Core and Mobility",
                Type = WorkoutType.PREBUILT
            };
            coreMobility.AddExercise(new TemplateExercise { Id = -401, WorkoutTemplateId = fallbackIdFour, Name = "Plank", MuscleGroup = MuscleGroup.CORE, TargetSets = 3, TargetReps = 60 });
            coreMobility.AddExercise(new TemplateExercise { Id = -402, WorkoutTemplateId = fallbackIdFour, Name = "Dead Bug", MuscleGroup = MuscleGroup.CORE, TargetSets = 3, TargetReps = 12 });
            coreMobility.AddExercise(new TemplateExercise { Id = -403, WorkoutTemplateId = fallbackIdFour, Name = "Hip Bridge", MuscleGroup = MuscleGroup.LEGS, TargetSets = 3, TargetReps = 15 });

            return new List<WorkoutTemplate>
            {
                fullBodyStrength,
                highIntensityIntervalTraining,
                pushPull,
                coreMobility
            };
        }

        private void InitializeDaySelection()
        {
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var defaultSelections = new[] { false, true, true, true, true, true, false };

            this.SelectedDays.Clear();
            for (int dayIndex = 0; dayIndex < CalendarIntegrationViewModel.TotalDaysInWeek; dayIndex++)
            {
                this.SelectedDays.Add(new DaySelectionItem(dayIndex, dayNames[dayIndex], defaultSelections[dayIndex]));
            }
        }

        private void LoadFallbackWorkouts(int clientId)
        {
            this.AvailableWorkouts.Clear();
            foreach (var workout in CalendarIntegrationViewModel.CreateFallbackWorkouts(clientId))
            {
                this.AvailableWorkouts.Add(workout);
            }

            int firstElementIndex = 0;
            if (this.AvailableWorkouts.Count > 0)
            {
                this.SelectedWorkout = this.AvailableWorkouts[firstElementIndex];
            }
        }
    }
}
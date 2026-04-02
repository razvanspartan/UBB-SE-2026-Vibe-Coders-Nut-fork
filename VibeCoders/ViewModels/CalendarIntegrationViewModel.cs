using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class DaySelectionItem : ObservableObject
    {
        private bool _isSelected;

        public int DayOfWeekIndex { get; }
        public string DayName { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public DaySelectionItem(int dayOfWeekIndex, string dayName, bool initialSelection = false)
        {
            DayOfWeekIndex = dayOfWeekIndex;
            DayName = dayName;
            _isSelected = initialSelection;
        }
    }

    public class CalendarIntegrationViewModel : ObservableObject
    {
        private readonly IDataStorage _dataStorage;
        private readonly ICalendarExportService _calendarExportService;
        private readonly IUserSession _userSession;

        private ObservableCollection<WorkoutTemplate> _availableWorkouts = new();
        private WorkoutTemplate? _selectedWorkout;
        private int _durationWeeks = 4;
        private ObservableCollection<DaySelectionItem> _selectedDays = new();
        private bool _isLoading;
        private string _generatedIcsContent = string.Empty;

        public ObservableCollection<WorkoutTemplate> AvailableWorkouts
        {
            get => _availableWorkouts;
            set => SetProperty(ref _availableWorkouts, value);
        }

        public WorkoutTemplate? SelectedWorkout
        {
            get => _selectedWorkout;
            set => SetProperty(ref _selectedWorkout, value);
        }

        public int DurationWeeks
        {
            get => _durationWeeks;
            set => SetProperty(ref _durationWeeks, value);
        }

        public ObservableCollection<DaySelectionItem> SelectedDays
        {
            get => _selectedDays;
            set => SetProperty(ref _selectedDays, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string GeneratedIcsContent
        {
            get => _generatedIcsContent;
            set => SetProperty(ref _generatedIcsContent, value);
        }

        public CalendarIntegrationViewModel(
            IDataStorage dataStorage,
            ICalendarExportService calendarExportService,
            IUserSession userSession)
        {
            _dataStorage = dataStorage ?? throw new ArgumentNullException(nameof(dataStorage));
            _calendarExportService = calendarExportService ?? throw new ArgumentNullException(nameof(calendarExportService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            InitializeDaySelection();

            // Populate immediately so UI is responsive even if DB is unavailable.
            var clientId = (int)_userSession.CurrentClientId;
            LoadFallbackWorkouts(clientId);

            // Try to refresh from DB in background.
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
                var dbLoadTask = Task.Run(() => _dataStorage.GetAvailableWorkouts(clientId));
                var completedTask = await Task.WhenAny(dbLoadTask, Task.Delay(1500));

                // If DB is slow/unavailable, keep fallback list and return quickly.
                if (completedTask != dbLoadTask)
                {
                    return;
                }

                var workouts = await dbLoadTask;

                AvailableWorkouts.Clear();
                foreach (var workout in workouts)
                {
                    AvailableWorkouts.Add(workout);
                }

                // Keep the calendar page testable even when DB returns no data.
                if (AvailableWorkouts.Count == 0)
                {
                    LoadFallbackWorkouts(clientId);
                    return;
                }

                if (SelectedWorkout == null && AvailableWorkouts.Count > 0)
                {
                    SelectedWorkout = AvailableWorkouts[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {ex.Message}");
                var clientId = (int)_userSession.CurrentClientId;
                LoadFallbackWorkouts(clientId);
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

        private static List<WorkoutTemplate> CreateFallbackWorkouts(int clientId)
        {
            var fullBodyStrength = new WorkoutTemplate
            {
                Id = -1,
                ClientId = clientId,
                Name = "Fallback - Full Body Strength",
                Type = WorkoutType.PREBUILT
            };
            fullBodyStrength.AddExercise(new TemplateExercise { Id = -101, WorkoutTemplateId = -1, Name = "Back Squat", MuscleGroup = MuscleGroup.LEGS, TargetSets = 4, TargetReps = 6, TargetWeight = 60 });
            fullBodyStrength.AddExercise(new TemplateExercise { Id = -102, WorkoutTemplateId = -1, Name = "Bench Press", MuscleGroup = MuscleGroup.CHEST, TargetSets = 4, TargetReps = 6, TargetWeight = 50 });
            fullBodyStrength.AddExercise(new TemplateExercise { Id = -103, WorkoutTemplateId = -1, Name = "Barbell Row", MuscleGroup = MuscleGroup.BACK, TargetSets = 4, TargetReps = 8, TargetWeight = 45 });

            var hiitConditioning = new WorkoutTemplate
            {
                Id = -2,
                ClientId = clientId,
                Name = "Fallback - HIIT Conditioning",
                Type = WorkoutType.PREBUILT
            };
            hiitConditioning.AddExercise(new TemplateExercise { Id = -201, WorkoutTemplateId = -2, Name = "Burpees", MuscleGroup = MuscleGroup.CORE, TargetSets = 4, TargetReps = 12, TargetWeight = 0 });
            hiitConditioning.AddExercise(new TemplateExercise { Id = -202, WorkoutTemplateId = -2, Name = "Jump Squats", MuscleGroup = MuscleGroup.LEGS, TargetSets = 4, TargetReps = 15, TargetWeight = 0 });
            hiitConditioning.AddExercise(new TemplateExercise { Id = -203, WorkoutTemplateId = -2, Name = "Mountain Climbers", MuscleGroup = MuscleGroup.CORE, TargetSets = 4, TargetReps = 20, TargetWeight = 0 });

            var pushPull = new WorkoutTemplate
            {
                Id = -3,
                ClientId = clientId,
                Name = "Fallback - Push Pull Split",
                Type = WorkoutType.PREBUILT
            };
            pushPull.AddExercise(new TemplateExercise { Id = -301, WorkoutTemplateId = -3, Name = "Overhead Press", MuscleGroup = MuscleGroup.SHOULDERS, TargetSets = 4, TargetReps = 8, TargetWeight = 35 });
            pushPull.AddExercise(new TemplateExercise { Id = -302, WorkoutTemplateId = -3, Name = "Pull-Ups", MuscleGroup = MuscleGroup.BACK, TargetSets = 4, TargetReps = 8, TargetWeight = 0 });
            pushPull.AddExercise(new TemplateExercise { Id = -303, WorkoutTemplateId = -3, Name = "Dumbbell Curl", MuscleGroup = MuscleGroup.ARMS, TargetSets = 3, TargetReps = 12, TargetWeight = 12 });

            var coreMobility = new WorkoutTemplate
            {
                Id = -4,
                ClientId = clientId,
                Name = "Fallback - Core and Mobility",
                Type = WorkoutType.PREBUILT
            };
            coreMobility.AddExercise(new TemplateExercise { Id = -401, WorkoutTemplateId = -4, Name = "Plank", MuscleGroup = MuscleGroup.CORE, TargetSets = 3, TargetReps = 60, TargetWeight = 0 });
            coreMobility.AddExercise(new TemplateExercise { Id = -402, WorkoutTemplateId = -4, Name = "Dead Bug", MuscleGroup = MuscleGroup.CORE, TargetSets = 3, TargetReps = 12, TargetWeight = 0 });
            coreMobility.AddExercise(new TemplateExercise { Id = -403, WorkoutTemplateId = -4, Name = "Hip Bridge", MuscleGroup = MuscleGroup.LEGS, TargetSets = 3, TargetReps = 15, TargetWeight = 0 });

            return new List<WorkoutTemplate>
            {
                fullBodyStrength,
                hiitConditioning,
                pushPull,
                coreMobility
            };
        }

        private void LoadFallbackWorkouts(int clientId)
        {
            AvailableWorkouts.Clear();
            foreach (var workout in CreateFallbackWorkouts(clientId))
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
            return SelectedDays
                .Where(d => d.IsSelected)
                .Select(d => d.DayOfWeekIndex)
                .ToArray();
        }

        public string? ValidateInput()
        {
            if (SelectedWorkout == null)
                return "Please select a workout from the dropdown.";

            if (DurationWeeks < 1 || DurationWeeks > 52)
                return "Duration must be between 1 and 52 weeks.";

            var selectedDaysArray = GetSelectedDaysOfWeek();
            if (selectedDaysArray.Length == 0)
                return "Please select at least one training day.";

            return null;
        }

        public async Task<string> GenerateCalendarAsync()
        {
            return await Task.Run(() =>
            {
                var validationError = ValidateInput();
                if (validationError != null)
                    throw new InvalidOperationException(validationError);

                if (SelectedWorkout == null)
                    throw new InvalidOperationException("No workout selected.");

                var selectedDaysArray = GetSelectedDaysOfWeek();
                var icsContent = _calendarExportService.GenerateCalendar(
                    SelectedWorkout,
                    DurationWeeks,
                    selectedDaysArray);

                GeneratedIcsContent = icsContent;
                return icsContent;
            });
        }

        public void ToggleDaySelection(int dayOfWeek)
        {
            var dayItem = SelectedDays.FirstOrDefault(d => d.DayOfWeekIndex == dayOfWeek);
            if (dayItem != null)
            {
                dayItem.IsSelected = !dayItem.IsSelected;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class CalendarIntegrationViewModelTests
    {
        private readonly ICalendarWorkoutCatalogService _workoutCatalogService;
        private readonly ICalendarExportService _calendarExportService;
        private readonly IUserSession _userSession;
        private readonly CalendarIntegrationViewModel _sut;

        public CalendarIntegrationViewModelTests()
        {
            _workoutCatalogService = Substitute.For<ICalendarWorkoutCatalogService>();
            _calendarExportService = Substitute.For<ICalendarExportService>();
            _userSession = Substitute.For<IUserSession>();

            _userSession.CurrentClientId.Returns(1);
            _workoutCatalogService.GetFallbackWorkouts(1).Returns(new List<WorkoutTemplate>());

            _sut = new CalendarIntegrationViewModel(_workoutCatalogService, _calendarExportService, _userSession);
        }

        [Fact]
        public void InitializeDaySelection_SetsDefaultDaysCorrectly()
        {
            // Assert (Initialization happens in constructor)
            _sut.SelectedDays.Should().HaveCount(7);

            // Check default selections (Sunday: false, Mon-Fri: true, Saturday: false)
            _sut.SelectedDays[0].IsSelected.Should().BeFalse(); // Sun
            _sut.SelectedDays[1].IsSelected.Should().BeTrue();  // Mon
            _sut.SelectedDays[2].IsSelected.Should().BeTrue();  // Tue
            _sut.SelectedDays[3].IsSelected.Should().BeTrue();  // Wed
            _sut.SelectedDays[4].IsSelected.Should().BeTrue();  // Thu
            _sut.SelectedDays[5].IsSelected.Should().BeTrue();  // Fri
            _sut.SelectedDays[6].IsSelected.Should().BeFalse(); // Sat
        }

        [Fact]
        public async Task LoadAvailableWorkoutsAsync_Success_PopulatesWorkouts()
        {
            // Arrange
            var workouts = new List<WorkoutTemplate>
            {
                new WorkoutTemplate { Id = 1, Name = "Workout A" },
                new WorkoutTemplate { Id = 2, Name = "Workout B" }
            };

            _workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>()).Returns(workouts);

            // Act
            await _sut.LoadAvailableWorkoutsAsync();

            // Assert
            _sut.AvailableWorkouts.Should().HaveCount(2);
            _sut.SelectedWorkout.Should().Be(workouts[0]);
            _sut.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_NotLoadingAndEmpty_CallsLoad()
        {
            // Arrange
            _sut.IsLoading = false;
            _sut.AvailableWorkouts.Clear();
            var workouts = new List<WorkoutTemplate> { new WorkoutTemplate { Id = 1 } };
            _workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>()).Returns(workouts);

            // Act
            await _sut.EnsureWorkoutsLoadedAsync();

            // Assert
            _sut.AvailableWorkouts.Should().HaveCount(1);
        }

        [Fact]
        public void GetSelectedDaysOfWeek_ReturnsCorrectIndexes()
        {
            // Arrange
            _sut.SelectedDays[0].IsSelected = true;  // Sun
            _sut.SelectedDays[1].IsSelected = false; // Mon
            _sut.SelectedDays[2].IsSelected = true;  // Tue

            // Act
            var result = _sut.GetSelectedDaysOfWeek();

            // Assert
            // Checking first three, others are default (Wed, Thu, Fri are true from init)
            result.Should().Contain(new[] { 0, 2, 3, 4, 5 });
            result.Should().NotContain(1);
        }

        [Fact]
        public void ValidateInput_NoWorkoutSelected_ReturnsErrorMessage()
        {
            // Arrange
            _sut.SelectedWorkout = null;

            // Act
            var result = _sut.ValidateInput();

            // Assert
            result.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public void ValidateInput_InvalidDuration_ReturnsErrorMessage()
        {
            // Arrange
            _sut.SelectedWorkout = new WorkoutTemplate();
            _sut.DurationWeeks = 0;

            // Act
            var result = _sut.ValidateInput();

            // Assert
            result.Should().Be("Duration must be between 1 and 52 weeks.");
        }

        [Fact]
        public void ValidateInput_NoDaysSelected_ReturnsErrorMessage()
        {
            // Arrange
            _sut.SelectedWorkout = new WorkoutTemplate();
            _sut.DurationWeeks = 4;
            foreach (var day in _sut.SelectedDays)
            {
                day.IsSelected = false;
            }

            // Act
            var result = _sut.ValidateInput();

            // Assert
            result.Should().Be("Please select at least one training day.");
        }

        [Fact]
        public void ValidateInput_ValidInput_ReturnsNull()
        {
            // Arrange
            _sut.SelectedWorkout = new WorkoutTemplate();
            _sut.DurationWeeks = 4;
            _sut.SelectedDays[1].IsSelected = true;

            // Act
            var result = _sut.ValidateInput();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GenerateCalendarAsync_ValidInput_SetsGeneratedContentAndReturnsIt()
        {
            // Arrange
            _sut.SelectedWorkout = new WorkoutTemplate { Id = 1 };
            _sut.DurationWeeks = 4;

            _calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), 4, Arg.Any<int[]>())
                .Returns("ICS CONTENT");

            // Act
            var result = await _sut.GenerateCalendarAsync();

            // Assert
            result.Should().Be("ICS CONTENT");
            _sut.GeneratedIcsContent.Should().Be("ICS CONTENT");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_ValidInput_ReturnsSuccessResult()
        {
            // Arrange
            _sut.SelectedWorkout = new WorkoutTemplate { Id = 1 };

            _calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), Arg.Any<int>(), Arg.Any<int[]>())
                .Returns("ICS CONTENT");

            // Act
            var result = await _sut.GenerateCalendarForExportAsync();

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.GeneratedCalendarContent.Should().Be("ICS CONTENT");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_ValidationFails_ReturnsFailureResult()
        {
            // Arrange
            _sut.SelectedWorkout = null; // Forces validation fail

            // Act
            var result = await _sut.GenerateCalendarForExportAsync();

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public async Task SaveGeneratedCalendarToDownloadsFallbackAsync_CallsService()
        {
            // Arrange
            _sut.GeneratedIcsContent = "ICS CONTENT";
            _sut.SelectedWorkout = new WorkoutTemplate { Name = "My Workout" };

            _calendarExportService.SaveCalendarToDownloadsAsync("ICS CONTENT", "My Workout")
                .Returns(Task.FromResult((string?)"C:\\Downloads\\My_Workout.ics"));

            // Act
            var result = await _sut.SaveGeneratedCalendarToDownloadsFallbackAsync();

            // Assert
            result.Should().Be("C:\\Downloads\\My_Workout.ics");
        }

        [Fact]
        public void SetErrorStatus_UpdatesStateCorrectly()
        {
            // Act
            _sut.SetErrorStatus("An error occurred");

            // Assert
            _sut.StatusSeverity.Should().Be(InfoBarSeverity.Error);
            _sut.StatusTitle.Should().Be("Error");
            _sut.StatusMessage.Should().Be("An error occurred");
            _sut.IsStatusOpen.Should().BeTrue();
        }

        [Fact]
        public void SetSuccessStatus_UpdatesStateCorrectly()
        {
            // Act
            _sut.SetSuccessStatus("Success message");

            // Assert
            _sut.StatusSeverity.Should().Be(InfoBarSeverity.Success);
            _sut.StatusTitle.Should().Be("Success");
            _sut.StatusMessage.Should().Be("Success message");
            _sut.IsStatusOpen.Should().BeTrue();
        }

        [Fact]
        public void ClearStatus_ResetsStateCorrectly()
        {
            // Arrange
            _sut.SetErrorStatus("An error occurred");

            // Act
            _sut.ClearStatus();

            // Assert
            _sut.StatusTitle.Should().BeEmpty();
            _sut.StatusMessage.Should().BeEmpty();
            _sut.IsStatusOpen.Should().BeFalse();
        }

        [Fact]
        public void ToggleDaySelection_TogglesSelectionCorrectly()
        {
            // Arrange
            bool initialState = _sut.SelectedDays[1].IsSelected; // Monday

            // Act
            _sut.ToggleDaySelection(1);

            // Assert
            _sut.SelectedDays[1].IsSelected.Should().Be(!initialState);
        }

        [Fact]
        public void ToggleDaySelection_UnknownDay_DoesNothing()
        {
            // Act
            var exception = Record.Exception(() => _sut.ToggleDaySelection(99));

            // Assert
            exception.Should().BeNull();
        }

        [Fact]
        public async Task LoadAvailableWorkoutsAsync_ExceptionThrown_SetsFallbackWorkouts()
        {
            // Arrange
            _workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>())
                .Returns(Task.FromException<IReadOnlyList<WorkoutTemplate>>(new Exception("Network failure")));

            var fallbackWorkouts = new List<WorkoutTemplate>
            {
                new WorkoutTemplate { Id = 99, Name = "Fallback" }
            };

            _workoutCatalogService.GetFallbackWorkouts(1).Returns(fallbackWorkouts);

            // Act
            await _sut.LoadAvailableWorkoutsAsync();

            // Assert
            _sut.AvailableWorkouts.Should().HaveCount(1);
            _sut.SelectedWorkout.Should().Be(fallbackWorkouts[0]);
            _sut.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_AlreadyLoading_DoesNotCallLoad()
        {
            // Arrange
            _workoutCatalogService.ClearReceivedCalls();
            _sut.IsLoading = true;
            _sut.AvailableWorkouts.Clear();

            // Act
            await _sut.EnsureWorkoutsLoadedAsync();

            // Assert
            await _workoutCatalogService.DidNotReceiveWithAnyArgs().GetAvailableWorkoutsAsync(default, default);
            _sut.AvailableWorkouts.Should().BeEmpty();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_AlreadyHasWorkouts_DoesNotCallLoad()
        {
            // Arrange
            _workoutCatalogService.ClearReceivedCalls();
            _sut.IsLoading = false;
            _sut.AvailableWorkouts.Add(new WorkoutTemplate { Id = 1 });

            // Act
            await _sut.EnsureWorkoutsLoadedAsync();

            // Assert
            await _workoutCatalogService.DidNotReceiveWithAnyArgs().GetAvailableWorkoutsAsync(default, default);
            _sut.AvailableWorkouts.Should().HaveCount(1);
        }

        [Fact]
        public async Task GenerateCalendarAsync_SelectedWorkoutNull_ThrowsException()
        {
            // Arrange
            _sut.SelectedWorkout = null; // Fails validation first

            // Act
            var exception = await Record.ExceptionAsync(async () => await _sut.GenerateCalendarAsync());

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_EmptyContent_ReturnsFailureResult()
        {
            // Arrange
            _sut.SelectedWorkout = new WorkoutTemplate { Id = 1 };
            _sut.DurationWeeks = 4;

            _calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), Arg.Any<int>(), Arg.Any<int[]>())
                .Returns(string.Empty);

            // Act
            var result = await _sut.GenerateCalendarForExportAsync();

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Be("Failed to generate calendar file. Please try again.");
        }

        [Fact]
        public async Task SaveGeneratedCalendarToDownloadsFallbackAsync_NullWorkout_UsesFallbackName()
        {
            // Arrange
            _sut.GeneratedIcsContent = "ICS CONTENT";
            _sut.SelectedWorkout = null;

            _calendarExportService.SaveCalendarToDownloadsAsync("ICS CONTENT", "Workout") // Checks it used "Workout" instead of null
                .Returns(Task.FromResult((string?)"C:\\Downloads\\Workout.ics"));

            // Act
            var result = await _sut.SaveGeneratedCalendarToDownloadsFallbackAsync();

            // Assert
            result.Should().Be("C:\\Downloads\\Workout.ics");
        }
    }
}
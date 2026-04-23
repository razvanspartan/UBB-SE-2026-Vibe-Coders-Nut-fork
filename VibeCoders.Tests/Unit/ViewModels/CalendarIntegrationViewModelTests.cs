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
        private readonly ICalendarWorkoutCatalogService workoutCatalogService;
        private readonly ICalendarExportService calendarExportService;
        private readonly IUserSession userSession;
        private readonly CalendarIntegrationViewModel systemUnderTest;

        public CalendarIntegrationViewModelTests()
        {
            this.workoutCatalogService = Substitute.For<ICalendarWorkoutCatalogService>();
            this.calendarExportService = Substitute.For<ICalendarExportService>();
            this.userSession = Substitute.For<IUserSession>();

            this.userSession.CurrentClientId.Returns(1);
            this.workoutCatalogService.GetFallbackWorkouts(1).Returns(new List<WorkoutTemplate>());

            this.systemUnderTest = new CalendarIntegrationViewModel(this.workoutCatalogService, this.calendarExportService, this.userSession);
        }

        [Fact]
        public void InitializeDaySelection_SetsDefaultDaysCorrectly()
        {
            this.systemUnderTest.SelectedDays.Should().HaveCount(7);

            this.systemUnderTest.SelectedDays[0].IsSelected.Should().BeFalse(); 
            this.systemUnderTest.SelectedDays[1].IsSelected.Should().BeTrue();  
            this.systemUnderTest.SelectedDays[2].IsSelected.Should().BeTrue();  
            this.systemUnderTest.SelectedDays[3].IsSelected.Should().BeTrue();  
            this.systemUnderTest.SelectedDays[4].IsSelected.Should().BeTrue();  
            this.systemUnderTest.SelectedDays[5].IsSelected.Should().BeTrue();  
            this.systemUnderTest.SelectedDays[6].IsSelected.Should().BeFalse(); 
        }

        [Fact]
        public async Task LoadAvailableWorkoutsAsync_Success_PopulatesWorkouts()
        {
            var workouts = new List<WorkoutTemplate>
            {
                new WorkoutTemplate { Id = 1, Name = "Workout A" },
                new WorkoutTemplate { Id = 2, Name = "Workout B" }
            };

            this.workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>()).Returns(workouts);

            await this.systemUnderTest.LoadAvailableWorkoutsAsync();

            this.systemUnderTest.AvailableWorkouts.Should().HaveCount(2);
            this.systemUnderTest.SelectedWorkout.Should().Be(workouts[0]);
            this.systemUnderTest.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_NotLoadingAndEmpty_CallsLoad()
        {
            this.systemUnderTest.IsLoading = false;
            this.systemUnderTest.AvailableWorkouts.Clear();
            var workouts = new List<WorkoutTemplate> { new WorkoutTemplate { Id = 1 } };
            this.workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>()).Returns(workouts);

            await this.systemUnderTest.EnsureWorkoutsLoadedAsync();

            this.systemUnderTest.AvailableWorkouts.Should().HaveCount(1);
        }

        [Fact]
        public void GetSelectedDaysOfWeek_ReturnsCorrectIndexes()
        {
            this.systemUnderTest.SelectedDays[0].IsSelected = true;  
            this.systemUnderTest.SelectedDays[1].IsSelected = false; 
            this.systemUnderTest.SelectedDays[2].IsSelected = true;  

            var result = this.systemUnderTest.GetSelectedDaysOfWeek();

            result.Should().Contain(new[] { 0, 2, 3, 4, 5 });
            result.Should().NotContain(1);
        }

        [Fact]
        public void ValidateInput_NoWorkoutSelected_ReturnsErrorMessage()
        {
            this.systemUnderTest.SelectedWorkout = null;

            var result = this.systemUnderTest.ValidateInput();

            result.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public void ValidateInput_InvalidDuration_ReturnsErrorMessage()
        {
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate();
            this.systemUnderTest.DurationWeeks = 0;

            var result = this.systemUnderTest.ValidateInput();

            result.Should().Be("Duration must be between 1 and 52 weeks.");
        }

        [Fact]
        public void ValidateInput_NoDaysSelected_ReturnsErrorMessage()
        {
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate();
            this.systemUnderTest.DurationWeeks = 4;
            foreach (var day in this.systemUnderTest.SelectedDays)
            {
                day.IsSelected = false;
            }

            var result = this.systemUnderTest.ValidateInput();

            result.Should().Be("Please select at least one training day.");
        }

        [Fact]
        public void ValidateInput_ValidInput_ReturnsNull()
        {
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate();
            this.systemUnderTest.DurationWeeks = 4;
            this.systemUnderTest.SelectedDays[1].IsSelected = true;

            var result = this.systemUnderTest.ValidateInput();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GenerateCalendarAsync_ValidInput_SetsGeneratedContentAndReturnsIt()
        {
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate { Id = 1 };
            this.systemUnderTest.DurationWeeks = 4;

            this.calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), 4, Arg.Any<int[]>())
                .Returns("ICS CONTENT");

            var result = await this.systemUnderTest.GenerateCalendarAsync();

            result.Should().Be("ICS CONTENT");
            this.systemUnderTest.GeneratedIcsContent.Should().Be("ICS CONTENT");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_ValidInput_ReturnsSuccessResult()
        {
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate { Id = 1 };

            this.calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), Arg.Any<int>(), Arg.Any<int[]>())
                .Returns("ICS CONTENT");

            var result = await this.systemUnderTest.GenerateCalendarForExportAsync();

            result.IsSuccessful.Should().BeTrue();
            result.GeneratedCalendarContent.Should().Be("ICS CONTENT");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_ValidationFails_ReturnsFailureResult()
        {
            this.systemUnderTest.SelectedWorkout = null;

            var result = await this.systemUnderTest.GenerateCalendarForExportAsync();

            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public async Task SaveGeneratedCalendarToDownloadsFallbackAsync_CallsService()
        {
            this.systemUnderTest.GeneratedIcsContent = "ICS CONTENT";
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate { Name = "My Workout" };

            this.calendarExportService.SaveCalendarToDownloadsAsync("ICS CONTENT", "My Workout")
                .Returns(Task.FromResult((string?)"C:\\Downloads\\My_Workout.ics"));

            var result = await this.systemUnderTest.SaveGeneratedCalendarToDownloadsFallbackAsync();

            result.Should().Be("C:\\Downloads\\My_Workout.ics");
        }

        [Fact]
        public void SetErrorStatus_UpdatesStateCorrectly()
        {
            this.systemUnderTest.SetErrorStatus("An error occurred");

            this.systemUnderTest.StatusSeverity.Should().Be(InfoBarSeverity.Error);
            this.systemUnderTest.StatusTitle.Should().Be("Error");
            this.systemUnderTest.StatusMessage.Should().Be("An error occurred");
            this.systemUnderTest.IsStatusOpen.Should().BeTrue();
        }

        [Fact]
        public void SetSuccessStatus_UpdatesStateCorrectly()
        {
            this.systemUnderTest.SetSuccessStatus("Success message");

            this.systemUnderTest.StatusSeverity.Should().Be(InfoBarSeverity.Success);
            this.systemUnderTest.StatusTitle.Should().Be("Success");
            this.systemUnderTest.StatusMessage.Should().Be("Success message");
            this.systemUnderTest.IsStatusOpen.Should().BeTrue();
        }

        [Fact]
        public void ClearStatus_ResetsStateCorrectly()
        {
            this.systemUnderTest.SetErrorStatus("An error occurred");

            this.systemUnderTest.ClearStatus();

            this.systemUnderTest.StatusTitle.Should().BeEmpty();
            this.systemUnderTest.StatusMessage.Should().BeEmpty();
            this.systemUnderTest.IsStatusOpen.Should().BeFalse();
        }

        [Fact]
        public void ToggleDaySelection_TogglesSelectionCorrectly()
        {
            bool initialState = this.systemUnderTest.SelectedDays[1].IsSelected; 

            this.systemUnderTest.ToggleDaySelection(1);

            this.systemUnderTest.SelectedDays[1].IsSelected.Should().Be(!initialState);
        }

        [Fact]
        public void ToggleDaySelection_UnknownDay_DoesNothing()
        {
            var exception = Record.Exception(() => this.systemUnderTest.ToggleDaySelection(99));

            exception.Should().BeNull();
        }

        [Fact]
        public async Task LoadAvailableWorkoutsAsync_ExceptionThrown_SetsFallbackWorkouts()
        {
            this.workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>())
                .Returns(Task.FromException<IReadOnlyList<WorkoutTemplate>>(new Exception("Network failure")));

            var fallbackWorkouts = new List<WorkoutTemplate>
            {
                new WorkoutTemplate { Id = 99, Name = "Fallback" }
            };

            this.workoutCatalogService.GetFallbackWorkouts(1).Returns(fallbackWorkouts);

            await this.systemUnderTest.LoadAvailableWorkoutsAsync();

            this.systemUnderTest.AvailableWorkouts.Should().HaveCount(1);
            this.systemUnderTest.SelectedWorkout.Should().Be(fallbackWorkouts[0]);
            this.systemUnderTest.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_AlreadyLoading_DoesNotCallLoad()
        {
            this.workoutCatalogService.ClearReceivedCalls();
            this.systemUnderTest.IsLoading = true;
            this.systemUnderTest.AvailableWorkouts.Clear();

            await this.systemUnderTest.EnsureWorkoutsLoadedAsync();

            await this.workoutCatalogService.DidNotReceiveWithAnyArgs().GetAvailableWorkoutsAsync(default, default);
            this.systemUnderTest.AvailableWorkouts.Should().BeEmpty();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_AlreadyHasWorkouts_DoesNotCallLoad()
        {
            this.workoutCatalogService.ClearReceivedCalls();
            this.systemUnderTest.IsLoading = false;
            this.systemUnderTest.AvailableWorkouts.Add(new WorkoutTemplate { Id = 1 });

            await this.systemUnderTest.EnsureWorkoutsLoadedAsync();

            await this.workoutCatalogService.DidNotReceiveWithAnyArgs().GetAvailableWorkoutsAsync(default, default);
            this.systemUnderTest.AvailableWorkouts.Should().HaveCount(1);
        }

        [Fact]
        public async Task GenerateCalendarAsync_SelectedWorkoutNull_ThrowsException()
        {
            this.systemUnderTest.SelectedWorkout = null;

            var exception = await Record.ExceptionAsync(async () => await this.systemUnderTest.GenerateCalendarAsync());

            exception.Should().NotBeNull();
            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_EmptyContent_ReturnsFailureResult()
        {
            this.systemUnderTest.SelectedWorkout = new WorkoutTemplate { Id = 1 };
            this.systemUnderTest.DurationWeeks = 4;

            this.calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), Arg.Any<int>(), Arg.Any<int[]>())
                .Returns(string.Empty);

            var result = await this.systemUnderTest.GenerateCalendarForExportAsync();

            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Be("Failed to generate calendar file. Please try again.");
        }

        [Fact]
        public async Task SaveGeneratedCalendarToDownloadsFallbackAsync_NullWorkout_UsesFallbackName()
        {
            this.systemUnderTest.GeneratedIcsContent = "ICS CONTENT";
            this.systemUnderTest.SelectedWorkout = null;

            this.calendarExportService.SaveCalendarToDownloadsAsync("ICS CONTENT", "Workout")
                .Returns(Task.FromResult((string?)"C:\\Downloads\\Workout.ics"));

            var result = await this.systemUnderTest.SaveGeneratedCalendarToDownloadsFallbackAsync();

            result.Should().Be("C:\\Downloads\\Workout.ics");
        }
    }
}